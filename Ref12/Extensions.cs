using System;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;

namespace SLaks.Ref12 {
	static class Extensions {
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
	}
}
