using System;
using Microsoft.RestrictedUsage.CSharp.Core;
using Microsoft.VisualStudio.Text;

namespace SLaks.Ref12.Services {
	static class CSharpLanguageUtilities {
		public static bool IsRunning() {
			bool result;
			try {
				NativeMethods.LangService_ServiceIsRunning(out result);
			} catch (EntryPointNotFoundException) {
				return true;    // If this API is not available (< VS2013), assume they are running
			}
			return result;
		}

		public static Position ToPosition(SnapshotPoint corePoint, ITextSnapshot snapshot = null) {
			SnapshotPoint snapshotPoint = corePoint.TranslateTo(snapshot ?? corePoint.Snapshot, PointTrackingMode.Positive);
			ITextSnapshotLine containingLine = snapshotPoint.GetContainingLine();
			int charIndex = Math.Max(snapshotPoint.Position - containingLine.Start.Position, 0);
			return new Position(containingLine.LineNumber, charIndex);
		}
		public static Tuple<int, int> ToPositionTuple(SnapshotPoint corePoint, ITextSnapshot snapshot = null) {
			var p = ToPosition(corePoint, snapshot);
			return Tuple.Create(p.Line, p.Character);
		}
	}
}