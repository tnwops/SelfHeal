using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Messaging;
using System.Threading;

using DataTransfer;

namespace SelfHeal {
	public abstract class MonitoredClass {
		public static Random RandomNumberGenerator = new Random();

		public ConcurrentCollection<string> ForceValidPass = new ConcurrentCollection<string>();
		public ConcurrentDictionary<string,List<ErrorCheck>> ErrorChecks = new ConcurrentDictionary<string, List<ErrorCheck>>();

		private MessageQueue ErrorCheckQueue;

		public MonitoredClass() {
			ErrorCheckQueue = MessageQueue.Exists( ".\\Private$\\ErrorChecks" ) ? new MessageQueue( ".\\Private$\\ErrorChecks" ) : MessageQueue.Create( ".\\Private$\\ErrorChecks" );
			ErrorCheckQueue.Formatter = new BinaryMessageFormatter();
			ErrorCheckQueue.ReceiveCompleted += new ReceiveCompletedEventHandler( RecievedErrorCheck );
			ErrorCheckQueue.BeginReceive();
		}

		private void RecievedErrorCheck( object sender, ReceiveCompletedEventArgs e ) {
			ErrorCheck _ErrorCheck = e.Message.Body as ErrorCheck;

			Microsoft.CSharp.CSharpCodeProvider _Provider = new Microsoft.CSharp.CSharpCodeProvider();
			System.CodeDom.Compiler.CompilerParameters _Parameters = new System.CodeDom.Compiler.CompilerParameters();
			_Parameters.GenerateExecutable = false;
			_Parameters.GenerateInMemory = true;
			System.CodeDom.Compiler.CompilerResults _Results = _Provider.CompileAssemblyFromSource( _Parameters, _ErrorCheck.TestFunction );
			if( _Results.Errors.Count == 0 ) {
				foreach( Type _Type in _Results.CompiledAssembly.GetExportedTypes() ) {
					object CompiledObject = _Results.CompiledAssembly.CreateInstance( _Type.FullName );
					System.Reflection.MethodInfo[] Methods = _Type.GetMethods( System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public );
					System.Reflection.MethodInfo CompiledTestFunction = Methods[0];
					_ErrorCheck.CompiledFunction = CompiledTestFunction;

					if( !ErrorChecks.ContainsKey( _ErrorCheck.Method ) )
						while( !ErrorChecks.TryAdd( _ErrorCheck.Method, new List<ErrorCheck>() ) )
							Thread.Sleep( 100 );

					ErrorChecks[_ErrorCheck.Method].Add( _ErrorCheck );
				}
			}

			ErrorCheckQueue.BeginReceive();
		}
	}
}