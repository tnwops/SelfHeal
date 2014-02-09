using System;
using System.Collections.Generic;

namespace DataTransfer {
	[Serializable]
	public class ValidPass {
		public string Function;
		public Dictionary<string, object> Data;

		public ValidPass() {
			Function = null;
			Data = new Dictionary<string, object>();
		}
	}
}
