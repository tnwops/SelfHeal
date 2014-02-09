namespace AnalyzerBase {
	public abstract class AbstractAnalyzerFactory<T> where T : IAnalyzer, new() {
		public IAnalyzer GetAnalyzer() {
			return new T();
		}
	}
}
