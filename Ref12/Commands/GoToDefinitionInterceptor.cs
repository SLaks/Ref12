using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using SLaks.Ref12.Services;

namespace SLaks.Ref12.Commands {
	class GoToDefinitionInterceptor : CommandTargetBase<VSConstants.VSStd97CmdID> {
		readonly IEnumerable<IReferenceSourceProvider> references;
		readonly DTE dte;
		readonly ITextDocument doc;

		public GoToDefinitionInterceptor(IEnumerable<IReferenceSourceProvider> references, IServiceProvider sp, IVsTextView adapter, IWpfTextView textView, ITextDocument doc) : base(adapter, textView, VSConstants.VSStd97CmdID.GotoDefn) {
			this.references = references;
			dte = (DTE)sp.GetService(typeof(DTE));
			this.doc = doc;
		}

		protected override bool Execute(VSConstants.VSStd97CmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
			SnapshotPoint? caretPoint = TextView.GetCaretPoint(s => s.ContentType.IsOfType("CSharp"));
			if (caretPoint == null)
				return false;

			var symbol = GetSymbol(doc.FilePath, caretPoint.Value);
			if (symbol == null)
				return false;

			var target = references.FirstOrDefault(r => r.AvailableAssemblies.Contains(symbol.Item1));
			if (target == null)
				return false;

			Debug.WriteLine("Ref12: Navigating to RQName " + symbol.Item2);

			target.Navigate(symbol.Item1, symbol.Item2);
			return true;
		}

		// Returns Assembly, RQName
		private Tuple<string, string> GetSymbol(string sourceFileName, SnapshotPoint caretPoint) {
			// Dev12 (VS2013) has the new simpler native API
			// Dev14 will hopefully have Roslyn
			// All other versions need ParseTreeNodes
			if (dte.Version == "12.0") {
				var dl = LanguageUtilities.GetGoToDefLocations(caretPoint, sourceFileName).FirstOrDefault();
				if (dl == null || !dl.IsMetadata)
					return null;

				return Tuple.Create(Path.GetFileNameWithoutExtension(dl.AssemblyBinaryName), dl.RQName);
			}

			var project = dte.Solution.FindProjectItem(sourceFileName).ContainingProject;
			var result = ParseTreeUtilities.GetNode(caretPoint, project, sourceFileName);
			if (result == null || result.DefinitionFiles.Any()) // Skip symbols in the current solution
				return null;
			return Tuple.Create(Path.GetFileNameWithoutExtension(result.AssemblyName), result.RQName);
		}

		protected override bool IsEnabled() {
			return false;   // Always pass through to the native check
		}
	}
}
