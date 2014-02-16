using System;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;

namespace SLaks.Ref12 {
	static class Extensions {
		public static ITextBuffer GetBufferContainingCaret(this ITextView textView, string contentType) {
			SnapshotPoint? caretPoint = textView.GetCaretPoint(s => s.ContentType.IsOfType(contentType));
			if (!caretPoint.HasValue)
				return null;
			return caretPoint.Value.Snapshot.TextBuffer;
		}
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

		public static SnapshotPoint? GetCaretPoint(this ITextView textView, ITextBuffer subjectBuffer) {
			CaretPosition position = textView.Caret.Position;
			SnapshotSpan? snapshotSpan = textView.BufferGraph.MapUpOrDownToBuffer(new SnapshotSpan(position.BufferPosition, 0), subjectBuffer);
			if (snapshotSpan.HasValue)
				return snapshotSpan.Value.Start;
			return null;
		}
		private static bool IsSourceBuffer(IProjectionBufferBase top, ITextBuffer bottom) {
			return top.SourceBuffers.Contains(bottom) || top.SourceBuffers.Any((ITextBuffer tb) => tb is IProjectionBufferBase && IsSourceBuffer((IProjectionBufferBase)tb, bottom));
		}

		public static BufferMapDirection ClassifyBufferMapDirection(ITextBuffer startBuffer, ITextBuffer destinationBuffer) {
			if (startBuffer == destinationBuffer) {
				return BufferMapDirection.Identity;
			}
			IProjectionBuffer projectionBuffer = startBuffer as IProjectionBuffer;
			if (projectionBuffer != null && IsSourceBuffer(projectionBuffer, destinationBuffer)) {
				return BufferMapDirection.Down;
			}
			IProjectionBuffer projectionBuffer2 = destinationBuffer as IProjectionBuffer;
			if (projectionBuffer2 != null && IsSourceBuffer(projectionBuffer2, startBuffer)) {
				return BufferMapDirection.Up;
			}
			return BufferMapDirection.Unrelated;
		}

		public static SnapshotSpan? MapUpOrDownToBuffer(this IBufferGraph bufferGraph, SnapshotSpan span, ITextBuffer targetBuffer) {
			switch (ClassifyBufferMapDirection(span.Snapshot.TextBuffer, targetBuffer)) {
				case BufferMapDirection.Identity:
					return new SnapshotSpan?(span);
				case BufferMapDirection.Down:
					return bufferGraph.MapDownToBuffer(span, SpanTrackingMode.EdgeExclusive, targetBuffer)
									  .Select(s => new SnapshotSpan?(s))
									  .FirstOrDefault();
				case BufferMapDirection.Up:
					return bufferGraph.MapUpToBuffer(span, SpanTrackingMode.EdgeExclusive, targetBuffer)
									  .Select(s => new SnapshotSpan?(s))
									  .FirstOrDefault();
				default:
					return null;
			}
		}
	}
	internal enum BufferMapDirection {
		Identity,
		Down,
		Up,
		Unrelated
	}
}
