using System;

namespace SelfHeal {
	public class MonitoredPropertyAttribute : Attribute {
		public string PropertyName {
			get;
			set;
		}

		public MonitoredPropertyAttribute( string p_PropertyName ) {
			PropertyName = p_PropertyName;
		}
	}
}
