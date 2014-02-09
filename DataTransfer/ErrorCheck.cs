using System;

namespace DataTransfer {
	[Serializable]
	public class ErrorCheck {
		public string Method;
		public ValueTypes Value1Type;
		public string Value1Name;
		public ValueTypes Value2Type;
		public string Value2Name;
		public object Value2Constant;
		public string Message;
		public string TestFunction;
		public System.Reflection.MethodInfo CompiledFunction;
	}
}
