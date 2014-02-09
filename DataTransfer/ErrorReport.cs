using System;
using System.Collections.Generic;

namespace DataTransfer {
	[Serializable]
	public class ErrorReport {
		public string Function;
		public string Exception;
		public Dictionary<string, object> Data;

		public ErrorReport() {
			Function = null;
			Exception = null;
			Data = new Dictionary<string, object>();
		}
	}
}
