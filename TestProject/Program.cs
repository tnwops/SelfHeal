using System;
using SelfHeal;
 
namespace ClassProtection {
	public class Program : MonitoredClass {
		// ONE EXCEPTION
		// 10 EXCEPTIONS
		// 100 EXCEPTIONS
		// 1,000 EXCEPTIONS
		// 10,000 EXCEPTIONS
		// 50,000 EXCEPTIONS
		static int[] EXCEPTIONS = { 1, 10, 100, 1000, 10000, 50000 };
		static int[] TRY_COUNT = { 1, 10, 100, 1000, 10000, 50000 };
		static int PROP_COUNT = 1;
		static int[] PARAM_COUNT = { 100000, 10000, 1000, 100, 10, 2 };

		public int Property {
			get;
			private set;
		}

		double FailFunction( int p_TestValue ) {
			return 1 / ( p_TestValue - Property );
		}

		[MonitoredProperty( "Property" )]
		[MonitoredMethod( double.PositiveInfinity )]
		double TestFunction( int p_TestValue ) {
			return 1 / ( p_TestValue - Property );
		}

		static void Main( string[] args ) {
			DateTime _StartTime, _EndTime;
			Console.WriteLine( "" );
			Console.WriteLine( "{0} Exceptions", EXCEPTIONS[0] );

			Program _Program = new Program();
			Console.WriteLine( "Fail Function" );
			_StartTime = DateTime.Now;
			Console.WriteLine( "Start Time: {0}", _StartTime.ToLongTimeString() );

			for( int _iter = 0; _iter < TRY_COUNT[0]; ++_iter ) {
				for( _Program.Property = 0; _Program.Property < PROP_COUNT; ++_Program.Property ) {
					for( int _param = 0; _param < PARAM_COUNT[0]; ++_param ) {
						Console.Write( "{0}\t{1}\t{2}\r", _iter, _Program.Property, _param );
						try {
							_Program.FailFunction( _param );
						} catch {
						}
					}
				}
			}
			
			_EndTime = DateTime.Now;
			Console.WriteLine( "  End Time: {0}", _EndTime.ToLongTimeString() );
			Console.WriteLine( "Total Time: {0}", ( _EndTime - _StartTime ).ToString() );
			Console.WriteLine();
			Console.WriteLine( "Test Function" );
			_StartTime = DateTime.Now;
			Console.WriteLine( "Start Time: {0}", _StartTime.ToLongTimeString() );

			for( int _iter = 0; _iter < TRY_COUNT[0]; ++_iter ) {
				for( _Program.Property = 0; _Program.Property < PROP_COUNT; ++_Program.Property ) {
					for( int _param = 0; _param < PARAM_COUNT[0]; ++_param ) {
						Console.Write( "{0}\t{1}\t{2}\r", _iter, _Program.Property, _param );
						_Program.TestFunction( _param );
					}
				}
			}

			_EndTime = DateTime.Now;
			Console.WriteLine( "  End Time: {0}", _EndTime.ToLongTimeString() );
			Console.WriteLine( "Total Time: {0}", ( _EndTime - _StartTime ).ToString() );

			Console.ReadKey();
		}
	}
}
