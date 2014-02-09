using System.Collections.Generic;

using DataTransfer;

namespace AnalyzerBase {
	public interface IAnalyzer {
		List<ErrorCheck> AnalyzeErrors( List<ErrorReport> p_ErrorReports, List<ValidPass> p_ValidPasses );
	}
}
