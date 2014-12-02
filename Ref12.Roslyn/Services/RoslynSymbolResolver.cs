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

			if (symbol.Kind == SymbolKind.Local || symbol.Kind == SymbolKind.Namespace)
				return null;

			// F12 on the declaration of a lambda parameter should jump to its type; all other parameters shouldn't be handled at all.
			var param = symbol as IParameterSymbol;
			if (param != null) {
				var method = param.ContainingSymbol as IMethodSymbol;
				if (method == null || method.MethodKind != MethodKind.LambdaMethod)
					return null;
				if (param.Locations.Length != 1)
					return null;

				if (param.Locations[0].IsInSource
				 && !param.Locations[0].SourceSpan.Contains(point)
				 && param.Locations[0].SourceSpan.End != point)		// Contains() is exclusive
					return null;
				else
					symbol = param.Type;
			}
			symbol = IndexIdTranslator.GetTargetSymbol(symbol);

			PortableExecutableReference reference = null;
			Compilation comp;
			if (doc.Project.TryGetCompilation(out comp))
				reference = comp.GetMetadataReference(symbol.ContainingAssembly) as PortableExecutableReference;

			return new MySymbolInfo(
				IndexIdTranslator.GetId(symbol),
				isLocal: doc.Project.Solution.Workspace.Kind != WorkspaceKind.MetadataAsSource && doc.Project.Solution.GetProject(symbol.ContainingAssembly) != null,
				assemblyPath: reference == null ? null : reference.Display,
				assemblyName: symbol.ContainingAssembly.Identity.Name
			);
		}
	}

}
