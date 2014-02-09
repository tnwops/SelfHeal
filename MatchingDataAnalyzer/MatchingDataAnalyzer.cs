using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using AnalyzerBase;
using DataTransfer;

namespace MatchingDataAnalyzer {
	public class MatchingDataAnalyzer : IAnalyzer {
		const string CodeToCompile = @"
namespace DynamicErrorChecks {
	public class RuntimeChecks {
			public static bool ErrorCheck( object lhs, object rhs ) {
				return lhs.Equals( rhs );
			}
	}
}";

		public List<ErrorCheck> AnalyzeErrors( List<ErrorReport> p_ErrorReports, List<ValidPass> p_ValidPasses ) {
			Func<object, object, bool> TestFunction = new Func<object, object, bool>( ( lhs, rhs ) => {
				return lhs.Equals( rhs );
			} );

			List<Tuple<string,string>> _Candidates = new List<Tuple<string, string>>();
			for( int _LeftIndex = 0; _LeftIndex < p_ErrorReports[0].Data.Count; ++_LeftIndex )
				for( int _RightIndex = _LeftIndex + 1; _RightIndex < p_ErrorReports[0].Data.Count; ++_RightIndex )
					_Candidates.Add(
						new Tuple<string, string>(
							p_ErrorReports[0].Data.Keys.ElementAt( _LeftIndex ),
							p_ErrorReports[0].Data.Keys.ElementAt( _RightIndex ) ) );

			foreach( ErrorReport _ErrorReport in p_ErrorReports.ToArray() )
				for( int _LeftIndex = 0; _LeftIndex < p_ErrorReports[0].Data.Count; ++_LeftIndex )
					for( int _RightIndex = _LeftIndex + 1; _RightIndex < p_ErrorReports[0].Data.Count; ++_RightIndex )
						if( !TestFunction( _ErrorReport.Data[_ErrorReport.Data.Keys.ElementAt( _LeftIndex )], _ErrorReport.Data[_ErrorReport.Data.Keys.ElementAt( _RightIndex )] ) )
							_Candidates.Remove( new Tuple<string, string>( _ErrorReport.Data.Keys.ElementAt( _LeftIndex ), _ErrorReport.Data.Keys.ElementAt( _RightIndex ) ) );

			List<ErrorCheck> _ErrorChecks = new List<ErrorCheck>();
			foreach( Tuple<string,string> _Match in _Candidates ) {
				ErrorCheck _ErrorCheck = new ErrorCheck();
				_ErrorCheck.Method = p_ErrorReports[0].Function;
//				_ErrorCheck.Operator = Operators.EQUALS;
				_ErrorCheck.Value1Type = ValueTypes.VARIABLE;
				_ErrorCheck.Value1Name = _Match.Item1;
				_ErrorCheck.Value2Type = ValueTypes.VARIABLE;
				_ErrorCheck.Value2Name = _Match.Item2;
				_ErrorCheck.Message = string.Format( "{0} cannot equal {1}", _Match.Item1, _Match.Item2 );
				_ErrorCheck.TestFunction = CodeToCompile;
				
				_ErrorChecks.Add( _ErrorCheck );
			}

			for( int _ErrorCheckIndex = 0; _ErrorCheckIndex < _ErrorChecks.Count; ++_ErrorCheckIndex ) {
				for( int _ValidPassIndex = 0; _ValidPassIndex < p_ValidPasses.Count; ++_ValidPassIndex ) {
					if( TestFunction( p_ValidPasses[_ValidPassIndex].Data[_ErrorChecks[_ErrorCheckIndex].Value1Name], p_ValidPasses[_ValidPassIndex].Data[_ErrorChecks[_ErrorCheckIndex].Value2Name] ) ) {
						_ErrorChecks.RemoveAt( _ErrorCheckIndex-- );
						break;
					}
				}
			}

			return _ErrorChecks;
		}
	}
}
