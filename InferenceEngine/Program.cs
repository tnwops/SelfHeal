using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Messaging;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using AnalyzerBase;
using DataTransfer;

namespace InferenceEngine {
	class Program {
		const string AssemblyPath = @"C:\dev\SelfHealPOC\ClassProtection\bin\Debug\analyzers";

		static ConcurrentDictionary<string,List<ValidPass>> GoodData;
		static ConcurrentDictionary<string,ConcurrentDictionary<string,List<ErrorReport>>> BadData;
		static ConcurrentDictionary<string,ConcurrentDictionary<string,int>> PastCount;

		static List<IAnalyzer> Analyzers;
		static List<Assembly> Assemblies;

		static MessageQueue _ErrorReportQueue;
		static MessageQueue _ErrorCheckQueue;
		static MessageQueue _ValidPassQueue;

		static void InitializeErrorReportQueue() {
			_ErrorReportQueue = MessageQueue.Exists( ".\\Private$\\ErrorReports" ) ? new MessageQueue( ".\\Private$\\ErrorReports" ) :MessageQueue.Create( ".\\Private$\\ErrorReports" );
			_ErrorReportQueue.Formatter = new BinaryMessageFormatter();
			_ErrorReportQueue.ReceiveCompleted += new ReceiveCompletedEventHandler( RecievedErrorReport );
			_ErrorReportQueue.BeginReceive();
		}

		static void InitializeValidPassQueue() {
			_ValidPassQueue = MessageQueue.Exists( ".\\Private$\\ValidPasses" ) ? new MessageQueue( ".\\Private$\\ValidPasses" ) : MessageQueue.Create( ".\\Private$\\ValidPasses" );
			_ValidPassQueue.Formatter = new BinaryMessageFormatter();
			_ValidPassQueue.ReceiveCompleted += new ReceiveCompletedEventHandler( RecievedValidPass );
			_ValidPassQueue.BeginReceive();
		}

		static void InitializeErrorCheckQueue() {
			_ErrorCheckQueue = MessageQueue.Exists( ".\\Private$\\ErrorChecks" ) ? new MessageQueue( ".\\Private$\\ErrorChecks" ) : MessageQueue.Create( ".\\Private$\\ErrorChecks" );
			_ErrorCheckQueue.Formatter = new BinaryMessageFormatter();
		}

		static void InitializeQueues() {
			InitializeErrorReportQueue();
			InitializeValidPassQueue();
			InitializeErrorCheckQueue();
		}

		static void LoadAssemblies() {
			foreach( string _FileName in Directory.EnumerateFiles( AssemblyPath, "*.dll", SearchOption.AllDirectories ) ) {
				Assemblies.Add( Assembly.LoadFrom( _FileName ) );
			}
		}

		static void LoadAnalyzers() {
			LoadAssemblies();
			foreach( Assembly _Assembly in Assemblies ) {
				foreach( Type _Type in _Assembly.GetExportedTypes() )
					if( _Type.FullName.EndsWith( "AnalyzerFactory" ) ) {
						object _Factory =_Assembly.CreateInstance( _Type.FullName );
						Analyzers.Add( _Type.InvokeMember( "GetAnalyzer", BindingFlags.InvokeMethod, null, _Factory, null ) as IAnalyzer );
					}
			}
		}

		static void Main( string[] args ) {
			Assemblies = new List<Assembly>();
			Analyzers = new List<IAnalyzer>();

			BadData = new ConcurrentDictionary<string, ConcurrentDictionary<string, List<ErrorReport>>>();
			GoodData = new ConcurrentDictionary<string, List<ValidPass>>();
			PastCount = new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>();

			InitializeQueues();
			LoadAnalyzers();

			while( true ) {
				if( Console.KeyAvailable )
					if( Console.ReadKey().Key == ConsoleKey.Escape )
						break;

				foreach( string _Function in BadData.Keys )
					foreach( string _Exception in BadData[_Function].Keys )
						if( BadData[_Function][_Exception].Count > PastCount[_Function][_Exception] ) {
							PastCount[_Function][_Exception] = BadData[_Function][_Exception].Count;
							if( BadData[_Function][_Exception].Count > 2 )
								foreach( IAnalyzer _Analyzer in Analyzers )
									foreach( ErrorCheck _ErrorCheck in _Analyzer.AnalyzeErrors( BadData[_Function][_Exception], GoodData[_Function] ) )
										Task.Factory.StartNew( () => {
											_ErrorCheckQueue.Send( new Message( _ErrorCheck, new BinaryMessageFormatter() ) );
											return;
										} );

						}

				Thread.Sleep( 100 );
			}
		}

		static void RecievedErrorReport( object sender, ReceiveCompletedEventArgs e ) {
			ErrorReport _ErrorReport = e.Message.Body as ErrorReport;

			if( !BadData.ContainsKey( _ErrorReport.Function ) ) {
				while( !BadData.TryAdd( _ErrorReport.Function, new ConcurrentDictionary<string, List<ErrorReport>>() ) );
				while( !PastCount.TryAdd( _ErrorReport.Function, new ConcurrentDictionary<string, int>() ) );
			}

			if( !BadData[_ErrorReport.Function].ContainsKey( _ErrorReport.Exception ) ) {
				while( !BadData[_ErrorReport.Function].TryAdd( _ErrorReport.Exception, new List<ErrorReport>() ) );
				while( !PastCount[_ErrorReport.Function].TryAdd( _ErrorReport.Exception, 0 ) );
			}

			BadData[_ErrorReport.Function][_ErrorReport.Exception].Add( _ErrorReport );
			_ErrorReportQueue.BeginReceive();
		}

		static void RecievedValidPass( object sender, ReceiveCompletedEventArgs e ) {
			ValidPass _ValidPass = e.Message.Body as ValidPass;

			if( !GoodData.ContainsKey( _ValidPass.Function ) ) {
				while( !GoodData.TryAdd( _ValidPass.Function, new List<ValidPass>() ) );
			}

			GoodData[_ValidPass.Function].Add( _ValidPass );

			_ValidPassQueue.BeginReceive();
		}
	}
}
