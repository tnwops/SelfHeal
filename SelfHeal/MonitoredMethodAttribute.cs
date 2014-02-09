using System;
using System.Messaging;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

using DataTransfer;
using PostSharp.Aspects;

namespace SelfHeal {
	[Serializable]
	public class MonitoredMethodAttribute : OnMethodBoundaryAspect {
		private delegate bool TestFunction( object lhs, object rhs );

		private object ReturnValueOnException {
			get;
			set;
		}

		public MonitoredMethodAttribute() {
			ReturnValueOnException = null;
		}

		public MonitoredMethodAttribute( object p_ReturnValueOnException ) {
			ReturnValueOnException = p_ReturnValueOnException;
		}

		public override bool CompileTimeValidate( MethodBase method ) {
			if( !method.DeclaringType.IsSubclassOf( typeof( MonitoredClass ) ) )
				throw new TargetException( "You may only flag methods with a MonitoredMethod if the containing class derives from MonitoredClass" );

			MethodInfo _Info = method as MethodInfo;
			if( null != method )
				if( !_Info.ReturnType.Equals( typeof( void ) ) )
					if( !_Info.ReturnType.IsAssignableFrom( ReturnValueOnException.GetType() ) ) {
						throw new TargetException( string.Format( "\nThe return type does not match the return value\nReturn Type: {0}\nReturn Value Type: {1}", _Info.ReturnType.ToString(), ReturnValueOnException.GetType().ToString() ) );
					}

			return base.CompileTimeValidate( method );
		}

		public sealed override void OnEntry( MethodExecutionArgs args ) {
			if( ( args.Instance as MonitoredClass ).ErrorChecks.ContainsKey( args.Method.ToString() ) ) {
				Parallel.ForEach( ( args.Instance as MonitoredClass ).ErrorChecks[args.Method.ToString()].ToArray(), _ErrorCheck => {
					object lhs = null, rhs = null;

					foreach( ParameterInfo _ParameterInfo in args.Method.GetParameters() ) {
						if( _ParameterInfo.Name == _ErrorCheck.Value1Name )
							lhs = args.Arguments[_ParameterInfo.Position];

						if( _ParameterInfo.Name == _ErrorCheck.Value2Name )
							rhs = args.Arguments[_ParameterInfo.Position];
					}

					if( null == lhs ) {
						PropertyInfo _Property = args.Instance.GetType().GetProperty( _ErrorCheck.Value1Name );
						if( null != _Property )
							lhs = _Property.GetValue( args.Instance, null );
					}

					if( null == rhs ) {
						if( _ErrorCheck.Value2Type == ValueTypes.CONSTANT ) {
							rhs = _ErrorCheck.Value2Constant;
						} else {
							PropertyInfo _Property = args.Instance.GetType().GetProperty( _ErrorCheck.Value2Name );
							if( null != _Property )
								rhs = _Property.GetValue( args.Instance, null );
						}
					}

					if( (bool)_ErrorCheck.CompiledFunction.Invoke( null, new object[] { lhs, rhs } ) ) {
						args.ReturnValue = ReturnValueOnException;
						args.FlowBehavior = FlowBehavior.Return;
					}
				} );
			}
		}

		public bool WeShouldCaptureAValidPass( MonitoredClass p_TargetClass, string p_TargetMethodName ) {
			if( p_TargetClass.ForceValidPass.Contains( p_TargetMethodName ) )
				return true;

			return MonitoredClass.RandomNumberGenerator.NextDouble() < .005;
		}

		public sealed override void OnSuccess( MethodExecutionArgs args ) {
			// There is a one half of one percent chance we will return a ValidPass
			// We will always return a ValidPass if the last pass was an exception
			if( WeShouldCaptureAValidPass( ( args.Instance as MonitoredClass ), args.Method.ToString() ) ) {
				ValidPass _ValidPass = new ValidPass();
				_ValidPass.Function = args.Method.ToString();

				foreach( ParameterInfo _ParameterInfo in args.Method.GetParameters() )
					_ValidPass.Data.Add( _ParameterInfo.Name, args.Arguments[_ParameterInfo.Position] );

				foreach( MonitoredPropertyAttribute _MonitoredProperty in args.Method.GetCustomAttributes( typeof( MonitoredPropertyAttribute ), false ) ) {
					PropertyInfo _PropertyInfo = args.Instance.GetType().GetProperty( _MonitoredProperty.PropertyName );
					_ValidPass.Data.Add( _PropertyInfo.Name, _PropertyInfo.GetValue( args.Instance, null ) );
				}

				Task.Factory.StartNew( () => {
					MessageQueue _ValidPassQueue = 
					MessageQueue.Exists( ".\\Private$\\ValidPasses" ) ? new MessageQueue( ".\\Private$\\ValidPasses" ) : MessageQueue.Create( ".\\Private$\\ValidPasses" );

					Message _ValidPassMessage = new Message( _ValidPass, new BinaryMessageFormatter() );
					_ValidPassQueue.Send( _ValidPassMessage );
				} );
				( args.Instance as MonitoredClass ).ForceValidPass.Remove( args.Method.ToString() );
			}

			base.OnSuccess( args );
		}

		public sealed override void OnException( MethodExecutionArgs args ) {
			ErrorReport _ErrorReport = new ErrorReport();
			_ErrorReport.Function = args.Exception.TargetSite.ToString();
			_ErrorReport.Exception = args.Exception.GetType().ToString();

			foreach( ParameterInfo _ParameterInfo in args.Method.GetParameters() ) {
				_ErrorReport.Data.Add( _ParameterInfo.Name, args.Arguments[_ParameterInfo.Position] );
			}

			foreach( MonitoredPropertyAttribute _MonitoredProperty in args.Method.GetCustomAttributes( typeof( MonitoredPropertyAttribute ), false ) ) {
				PropertyInfo _PropertyInfo = args.Instance.GetType().GetProperty( _MonitoredProperty.PropertyName );
				_ErrorReport.Data.Add( _PropertyInfo.Name, _PropertyInfo.GetValue( args.Instance, null ) );
			}

			Task.Factory.StartNew( () => {
				MessageQueue _ErrorReportsQueue = 
				MessageQueue.Exists( ".\\Private$\\ErrorReports" ) ? new MessageQueue( ".\\Private$\\ErrorReports" ) : MessageQueue.Create( ".\\Private$\\ErrorReports" );

				Message _ErrorReportMessage = new Message( _ErrorReport, new BinaryMessageFormatter() );
				_ErrorReportsQueue.Send( _ErrorReportMessage );
			} );

			( args.Instance as MonitoredClass ).ForceValidPass.Add( args.Method.ToString() );

			args.ReturnValue = ReturnValueOnException;
			args.FlowBehavior = FlowBehavior.Return;
			base.OnException( args );
		}
	}
}