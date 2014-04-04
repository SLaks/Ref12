using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Threading;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using SLaks.Ref12.Services;

namespace SLaks.Ref12.Commands {
	[Export(typeof(IWpfTextViewConnectionListener))]
	[ContentType("Basic")]
	[ContentType("CSharp")]
	[TextViewRole(PredefinedTextViewRoles.Document)]
	public class TextViewListener : IWpfTextViewConnectionListener {
		[Import]
		public SVsServiceProvider ServiceProvider { get; set; }

		[ImportMany(AllowRecomposition = true)] // For unit tests
		public IEnumerable<IReferenceSourceProvider> ReferenceProviders { get; set; }

		[Import]
		public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }
		[Import]
		public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

		public async void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers) {
			if (!subjectBuffers.Any(b => b.ContentType.IsOfType("CSharp") || b.ContentType.IsOfType("Basic")))
				return;

			// VS2010 only creates TextViewAdapters later; wait for it to exist.
			await Dispatcher.Yield();

			var textViewAdapter = EditorAdaptersFactoryService.GetViewAdapter(textView);
			if (textViewAdapter == null)
				return;
			ITextDocument document;
			if (!TextDocumentFactoryService.TryGetTextDocument(textView.TextDataModel.DocumentBuffer, out document))
				return;

			textView.Properties.GetOrCreateSingletonProperty(() => new GoToDefinitionInterceptor(ReferenceProviders, ServiceProvider, textViewAdapter, textView, document));
		}
		public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers) {
		}
	}
}
