using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.RestrictedUsage.CSharp.Core;
using Microsoft.VisualStudio.Text;

namespace SLaks.Ref12.Services {
	static class LanguageUtilities {
		public static bool IsRunning() {
			bool result;
			NativeMethods.LangService_ServiceIsRunning(out result);
			return result;
		}


		// Stolen from Microsoft.VisualStudio.CSharp.Services.Language.Features.Peek.PeekableItemSource
		public static Position ToCSharpPosition(SnapshotPoint corePoint, ITextSnapshot snapshot = null) {
			SnapshotPoint snapshotPoint = corePoint.TranslateTo(snapshot ?? corePoint.Snapshot, PointTrackingMode.Positive);
			ITextSnapshotLine containingLine = snapshotPoint.GetContainingLine();
			int charIndex = Math.Max(snapshotPoint.Position - containingLine.Start.Position, 0);
			return new Position(containingLine.LineNumber, charIndex);
		}

		public static IEnumerable<GoToDefLocation> GetGoToDefLocations(SnapshotPoint triggerPoint, string sourceFileName) {
			Position position = ToCSharpPosition(triggerPoint, null);

			string[] fileNames, rqNames, assemblyBinaryNames;
			int[] lines, columns;
			bool[] isMetaDataFlags;
			try {
				NativeMethods.GoToDefinition_GetLocations(position.Line, position.Character, sourceFileName, out fileNames, out lines, out columns, out rqNames, out assemblyBinaryNames, out isMetaDataFlags);
			} catch (InvalidOperationException) {
				yield break;
			}
			if (fileNames == null || lines == null || columns == null || rqNames == null || assemblyBinaryNames == null)
				yield break;

			if (fileNames.Length != lines.Length || lines.Length != columns.Length || columns.Length != rqNames.Length || rqNames.Length != assemblyBinaryNames.Length)
				throw new InvalidOperationException("GoToDefinition_GetLocations returned inconsistent arrays");

			for (int i = 0; i < fileNames.Length; i++)
				yield return new GoToDefLocation(fileNames[i], rqNames[i], assemblyBinaryNames[i], isMetaDataFlags[i]);
		}

	}
	static partial class NativeMethods {
		[DllImport("CSLangSvc.dll", PreserveSig = false)]
		internal static extern void LangService_ServiceIsRunning([MarshalAs(UnmanagedType.U1)] out bool fServiceIsRunning);

		[DllImport("CSLangSvc.dll", PreserveSig = false)]
		internal static extern void GoToDefinition_GetLocations(int iLine, int iCol, [MarshalAs(UnmanagedType.BStr)] string bstrFileName, [MarshalAs(UnmanagedType.SafeArray)] out string[] fileNames, [MarshalAs(UnmanagedType.SafeArray)] out int[] lines, [MarshalAs(UnmanagedType.SafeArray)] out int[] columns, [MarshalAs(UnmanagedType.SafeArray)] out string[] rqNames, [MarshalAs(UnmanagedType.SafeArray)] out string[] assemblyBinaryNames, [MarshalAs(UnmanagedType.SafeArray)] out bool[] isMetaDataFlags);
	}

	// Fields cannot use types from Microsoft.VisualStudio.CSharp.Services.Language.dll,
	// because my DLL is loaded before it, and it has no <bindingRedirect>s. Using these
	// types in local variables is perfectly fine.
	class GoToDefLocation {
		public string FileName { get; private set; }
		public string RQName { get; private set; }
		public string AssemblyBinaryName { get; private set; }
		public bool IsMetadata { get; private set; }
		public GoToDefLocation(string fileName, string rqName, string assemblyBinaryName, bool isMetadata) {
			this.FileName = fileName;
			this.RQName = rqName;
			this.AssemblyBinaryName = assemblyBinaryName;
			this.IsMetadata = isMetadata;
		}
	}
}
