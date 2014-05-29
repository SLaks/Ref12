using System;
using System.Globalization;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;

namespace SLaks.Ref12 {
	public static class Extensions {
		public static SnapshotPoint? GetCaretPoint(this ITextView textView, Predicate<ITextSnapshot> match) {
			CaretPosition position = textView.Caret.Position;
			SnapshotSpan? snapshotSpan = textView.BufferGraph.MapUpOrDownToFirstMatch(new SnapshotSpan(position.BufferPosition, 0), match);
			if (snapshotSpan.HasValue)
				return new SnapshotPoint?(snapshotSpan.Value.Start);
			return null;
		}
		public static SnapshotSpan? MapUpOrDownToFirstMatch(this IBufferGraph bufferGraph, SnapshotSpan span, Predicate<ITextSnapshot> match) {
			NormalizedSnapshotSpanCollection spans = bufferGraph.MapDownToFirstMatch(span, SpanTrackingMode.EdgeExclusive, match);
			if (!spans.Any())
				spans = bufferGraph.MapUpToFirstMatch(span, SpanTrackingMode.EdgeExclusive, match);
			return spans.Select(s => new SnapshotSpan?(s))
						.FirstOrDefault();
		}

		private static bool IsSourceBuffer(IProjectionBufferBase top, ITextBuffer bottom) {
			return top.SourceBuffers.Contains(bottom) || top.SourceBuffers.Any((ITextBuffer tb) => tb is IProjectionBufferBase && IsSourceBuffer((IProjectionBufferBase)tb, bottom));
		}

		public static void Execute(this IOleCommandTarget target, Enum commandId, uint execOptions = 0, IntPtr inHandle = default(IntPtr), IntPtr outHandle = default(IntPtr)) {
			var c = commandId.GetType().GUID;
			ErrorHandler.ThrowOnFailure(target.Exec(ref c, Convert.ToUInt32(commandId, CultureInfo.InvariantCulture), execOptions, inHandle, outHandle));
		}
	}
}
