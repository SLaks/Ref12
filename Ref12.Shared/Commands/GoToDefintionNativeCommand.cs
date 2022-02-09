using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace SLaks.Ref12.Commands {
	class GoToDefintionNativeCommand : CommandTargetBase<Ref12Command> {
		public GoToDefintionNativeCommand(IVsTextView adapter, IWpfTextView textView) : base(adapter, textView, Ref12Command.GoToDefinitionNative) {
		}
		protected override bool Execute(Ref12Command commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
			NextTarget.Execute(VSConstants.VSStd97CmdID.GotoDefn, nCmdexecopt, pvaIn, pvaOut);
			return true;
		}

		protected override bool IsEnabled() {
			// We override QueryStatus directly to pass the raw arguments
			// to the inner command, so this method will never be called.
			throw new NotImplementedException();
		}
		public override int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
			if (pguidCmdGroup != CommandGroup || cCmds != 1 || prgCmds[0].cmdID != CommandIds[0])
				return NextTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
			var innerGuid = typeof(VSConstants.VSStd97CmdID).GUID;
			var innerCommands = new[] { new OLECMD {
				cmdID = (uint)VSConstants.VSStd97CmdID.GotoDefn,
				cmdf=prgCmds[0].cmdf
			} };
			int result = NextTarget.QueryStatus(ref innerGuid, 1, innerCommands, pCmdText);
			prgCmds[0].cmdf = innerCommands[0].cmdf;
			return result;
		}
	}
}
