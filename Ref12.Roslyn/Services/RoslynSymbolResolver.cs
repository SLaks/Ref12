using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

using MySymbolInfo = SLaks.Ref12.Services.SymbolInfo;

namespace SLaks.Ref12.Services {
	public class RoslynSymbolResolver : ISymbolResolver {
		public MySymbolInfo GetSymbolAt(string sourceFileName, SnapshotPoint point) {
			// Yes; this is evil and synchronously waits for async tasks.
			// That is exactly what Roslyn's GoToDefinitionCommandHandler
			// does; apparently a VS command handler can't be truly async
			// (Roslyn does use IWaitIndicator, which I can't).

			var doc = point.Snapshot.GetOpenDocumentInCurrentContextWithChanges();
			var model = doc.GetSemanticModelAsync().Result;
			var symbol = SymbolFinder.FindSymbolAtPosition(model, point, doc.Project.Solution.Workspace);
			if (symbol == null || symbol.ContainingAssembly == null)
				return null;


			PortableExecutableReference reference = null;
			Compilation comp;
			if (doc.Project.TryGetCompilation(out comp))
				reference = comp.GetMetadataReference(symbol.ContainingAssembly) as PortableExecutableReference;

			return new MySymbolInfo(
				IndexIdTranslator.GetId(symbol),
				isLocal: doc.Project.Solution.Workspace.Kind != WorkspaceKind.MetadataAsSource && doc.Project.Solution.GetProject(symbol.ContainingAssembly) != null,
				assemblyPath: reference == null ? null : reference.FullPath,
				assemblyName: symbol.ContainingAssembly.Identity.Name
			);
		}
	}

}
