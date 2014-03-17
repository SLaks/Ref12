using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		readonly ITextDocument doc;
		readonly Dictionary<string, ISymbolResolver> resolvers = new Dictionary<string, ISymbolResolver>();

		public GoToDefinitionInterceptor(IEnumerable<IReferenceSourceProvider> references, IServiceProvider sp, IVsTextView adapter, IWpfTextView textView, ITextDocument doc) : base(adapter, textView, VSConstants.VSStd97CmdID.GotoDefn) {
			this.references = references;
			this.doc = doc;

			var dte = (DTE)sp.GetService(typeof(DTE));

			// Dev12 (VS2013) has the new simpler native API
			// Dev14 will hopefully have Roslyn
			// All other versions need ParseTreeNodes
			if (dte.Version == "12.0")
				resolvers.Add("CSharp", new CSharp12Resolver());
			else
				resolvers.Add("CSharp", new CSharp10Resolver(dte));
		}

		protected override bool Execute(VSConstants.VSStd97CmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
			ISymbolResolver resolver = null;
			SnapshotPoint? caretPoint = TextView.GetCaretPoint(s => resolvers.TryGetValue(s.ContentType.TypeName, out resolver));
			if (caretPoint == null)
				return false;

			var symbol = resolver.GetSymbolAt(doc.FilePath, caretPoint.Value);
			if (symbol == null)
				return false;

			var target = references.FirstOrDefault(r => r.AvailableAssemblies.Contains(symbol.AssemblyName));
			if (target == null)
				return false;

			Debug.WriteLine("Ref12: Navigating to IndexID " + symbol.IndexId);

			target.Navigate(symbol);
			return true;
		}

		protected override bool IsEnabled() {
			return false;   // Always pass through to the native check
		}
	}
}
