﻿using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace SLaks.Ref12.Commands {
	internal abstract class CommandTargetBase<TCommandEnum> : IOleCommandTarget where TCommandEnum : struct, IComparable {
		private IOleCommandTarget nextCommandTarget;
		protected readonly ITextView TextView;

		public Guid CommandGroup { get; set; }
		public ReadOnlyCollection<uint> CommandIds { get; private set; }
		protected IOleCommandTarget NextTarget { get { return nextCommandTarget; } }

		public CommandTargetBase(IVsTextView adapter, ITextView textView, params TCommandEnum[] commandIds) : this(adapter, textView, typeof(TCommandEnum).GUID, Array.ConvertAll(commandIds, e => Convert.ToUInt32(e))) { }
		public CommandTargetBase(IVsTextView adapter, ITextView textView, Guid commandGroup, params uint[] commandIds) {
			CommandGroup = commandGroup;
			CommandIds = new ReadOnlyCollection<uint>(commandIds);
			TextView = textView;

			Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => {
				// Add the target later to make sure it makes it in before other command handlers
				ErrorHandler.ThrowOnFailure(adapter.AddCommandFilter(this, out nextCommandTarget));
			}), DispatcherPriority.ApplicationIdle, null);
		}

		protected abstract bool IsEnabled();
		protected abstract bool Execute(TCommandEnum commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut);

		public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut) {
			if (pguidCmdGroup == CommandGroup && CommandIds.Contains(nCmdID)) {
				bool result = Execute((TCommandEnum)(object)(int)nCmdID, nCmdexecopt, pvaIn, pvaOut);

				if (result) {
					return VSConstants.S_OK;
				}
			}

			return nextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
		}

		public virtual int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText) {
			if (pguidCmdGroup != CommandGroup)
				return nextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);

			for (int i = 0; i < cCmds; i++) {
				if (CommandIds.Contains(prgCmds[i].cmdID)) {
					if (IsEnabled()) {
						prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
						return VSConstants.S_OK;
					}

					prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
				}
			}

			return nextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
		}
	}
}
