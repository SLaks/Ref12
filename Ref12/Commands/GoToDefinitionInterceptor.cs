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
			if (!LanguageUtilities.IsRunning())
				return false;

			SnapshotPoint? caretPoint = TextView.GetCaretPoint(s => s.ContentType.IsOfType("CSharp") || s.ContentType.IsOfType("Basic"));
			if (caretPoint == null)
				return false;

			var project = dte.Solution.FindProjectItem(doc.FilePath).ContainingProject;

			var dl = LanguageUtilities.GetGoToDefLocations(caretPoint.Value, doc).FirstOrDefault();
			if (dl == null || !dl.IsMetadata)
				return false;

			var assembly = Path.GetFileNameWithoutExtension(dl.AssemblyBinaryName);
			var target = references.FirstOrDefault(r => r.AvailableAssemblies.Contains(assembly));
			if (target == null)
				return false;

			Debug.WriteLine("Navigating to RQName " + dl.RQName);
			Debug.WriteLine(string.Join("\n", dte.ContextAttributes.Cast<ContextAttribute>().Select(c => c.Name + ": " + c.Values)));

			target.Navigate(assembly, dl.RQName);
			return true;
		}

		protected override bool IsEnabled() {
			return false;   // Always pass through to the native check
		}
	}
}
