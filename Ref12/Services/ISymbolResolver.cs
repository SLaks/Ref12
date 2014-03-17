using System.IO;
using Microsoft.VisualStudio.Text;

namespace SLaks.Ref12.Services {
	public interface ISymbolResolver {
		SymbolInfo GetSymbolAt(string sourceFileName, SnapshotPoint point);
	}
	public class SymbolInfo {
		public SymbolInfo(string indexId, string assemblyPath) : this(indexId, assemblyPath, Path.GetFileNameWithoutExtension(assemblyPath)) { }
		public SymbolInfo(string indexId, string assemblyPath, string assemblyName) {
			this.IndexId = indexId;
			this.AssemblyPath = assemblyPath;
			this.AssemblyName = assemblyName;
		}

		public string IndexId { get; private set; }
		public string AssemblyPath { get; private set; }
		public string AssemblyName { get; private set; }
	}
}
