using System.IO;
using Microsoft.VisualStudio.Text;

namespace SLaks.Ref12.Services {
	public interface ISymbolResolver {
		SymbolInfo GetSymbolAt(string sourceFileName, SnapshotPoint point);
	}
	public class SymbolInfo {
		public SymbolInfo(string indexId, bool isLocal, string assemblyPath) : this(indexId, isLocal, assemblyPath, Path.GetFileNameWithoutExtension(assemblyPath)) { }
		public SymbolInfo(string indexId, bool isLocal, string assemblyPath, string assemblyName) {
			this.IndexId = indexId;
			this.AssemblyPath = assemblyPath;
			this.AssemblyName = assemblyName;
			this.HasLocalSource = isLocal;
		}

		public string IndexId { get; private set; }
		public string AssemblyPath { get; private set; }
		public string AssemblyName { get; private set; }

		///<summary>Indicates whether this symbol is defined in the current solution.</summary>
		public bool HasLocalSource { get; private set; }
	}
}
