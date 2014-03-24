using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Text;

namespace SLaks.Ref12.Services {
	public class CSharp12Resolver : ISymbolResolver {
		static CSharp12Resolver() {
			AssemblyRedirector.Register();
		}

		public SymbolInfo GetSymbolAt(string sourceFileName, SnapshotPoint point) {
			var result = GetGoToDefLocations(sourceFileName, point).FirstOrDefault();
			if (result == null)
				return null;
			return new SymbolInfo(RQNameTranslator.ToIndexId(result.RQName), !result.IsMetadata, result.AssemblyBinaryName);
		}


		// Stolen from Microsoft.VisualStudio.CSharp.Services.Language.Features.Peek.PeekableItemSource

		static IEnumerable<GoToDefLocation> GetGoToDefLocations(string sourceFileName, SnapshotPoint point) {
			// I cannot use Position, because it will be compiled into a
			// a field in the iterator type, which will throw a TypeLoad
			// exception before I add my AssemblyResolve handler.
			var position = CSharpLanguageUtilities.ToPositionTuple(point, null);

			string[] fileNames, rqNames, assemblyBinaryNames;
			int[] lines, columns;
			bool[] isMetaDataFlags;
			try {
				NativeMethods.GoToDefinition_GetLocations(position.Item1, position.Item2, sourceFileName, out fileNames, out lines, out columns, out rqNames, out assemblyBinaryNames, out isMetaDataFlags);
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

		// Fields cannot use types from Microsoft.VisualStudio.CSharp.Services.Language.dll,
		// because my DLL is loaded before it, and it has no <bindingRedirect>s. Using these
		// types in local variables works fine, since we handle AssemblyResolve and load the
		// correct version before those methods are JITted.
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
	static class NativeMethods {
		[DllImport("CSLangSvc.dll", PreserveSig = false)]
		internal static extern void LangService_ServiceIsRunning([MarshalAs(UnmanagedType.U1)] out bool fServiceIsRunning);

		[DllImport("CSLangSvc.dll", PreserveSig = false)]
		internal static extern void GoToDefinition_GetLocations(int iLine, int iCol, [MarshalAs(UnmanagedType.BStr)] string bstrFileName, [MarshalAs(UnmanagedType.SafeArray)] out string[] fileNames, [MarshalAs(UnmanagedType.SafeArray)] out int[] lines, [MarshalAs(UnmanagedType.SafeArray)] out int[] columns, [MarshalAs(UnmanagedType.SafeArray)] out string[] rqNames, [MarshalAs(UnmanagedType.SafeArray)] out string[] assemblyBinaryNames, [MarshalAs(UnmanagedType.SafeArray)] out bool[] isMetaDataFlags);
	}
}
