using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.RestrictedUsage.CSharp.Compiler.IDE;
using Microsoft.RestrictedUsage.CSharp.Core;
using Microsoft.RestrictedUsage.CSharp.Extensions;
using Microsoft.RestrictedUsage.CSharp.Syntax;
using Microsoft.VisualStudio.CSharp.Services.Language.Interop;
using Microsoft.VisualStudio.CSharp.Services.Language.Refactoring;
using Microsoft.VisualStudio.Text;

namespace SLaks.Ref12.Services {
	class CSharp10Resolver : ISymbolResolver {
		static CSharp10Resolver() {
			AssemblyRedirector.TargetNames.Add("Microsoft.VisualStudio.CSharp.Services.Language");
			AssemblyRedirector.TargetNames.Add("Microsoft.VisualStudio.CSharp.Services.Language.Interop");
		}

		private readonly Lazy<IDECompilerHost> compilerHost = new Lazy<IDECompilerHost>();
		private readonly DTE dte;

		public CSharp10Resolver(DTE dte) {
			this.dte = dte;
		}

		public SymbolInfo GetSymbolAt(string sourceFileName, SnapshotPoint point) {
			var project = dte.Solution.FindProjectItem(sourceFileName).ContainingProject;
			var result = GetNode(point, project, sourceFileName);
			if (result == null || result.DefinitionFiles.Any()) // Skip symbols in the current solution
				return null;
			return new SymbolInfo(RQNameTranslator.ToIndexId(result.RQName), result.AssemblyName);
		}

		private NativeMethods.FindSourceDefinitionsAndDetermineSymbolResult GetNode(SnapshotPoint point, Project project, string fileName) {
			var compiler = compilerHost.Value.CreateCompiler(project);
			var sourceFile = compiler.SourceFiles[new FileName(fileName)];

			var node = sourceFile.GetParseTree().FindLeafNode(CSharpLanguageUtilities.ToPosition(point));
			if (node == null) return null;

			var rNode = ParseTreeMatch.GetReferencedNode(node);
			if (rNode == null) return null;
			return NativeMethods.FindSourceDefinitionsAndDetermineSymbolFromParseTree((IDECompilation)compiler.GetCompilation(), null, rNode);
		}

		static class NativeMethods {

			// Fields cannot use types from Microsoft.VisualStudio.CSharp.Services.Language.dll,
			// because my DLL is loaded before it, and it has no <bindingRedirect>s. Using these
			// types in local variables works fine, since we handle AssemblyResolve and load the
			// correct version before those methods are JITted.
			internal class SourceDefinitionOutputs : NodeAndFileNameArrayOutputs {
				//public NamedSymbolKind definitionKind;
				public int hasExternalVisibility;
			}
			internal class NodeAndFileNameArrayOutputs {
				public string[] fileNames;
				public IntPtr[] nodeOwners;
				public IntPtr[] nodePointers;
			}
			internal class SymbolInfoHolder {
				public string rqName;
				public string RQNameForParameterFromOtherPartialMethod;
				public string assemblyName;
				public string[] namespaceDefiningAssemblies;
				//public ParseTree.Handle anonymousTypePropertyRefOwnerHandle;
				public IntPtr anonymousTypePropertyRefNodePointer;
				public int[] anonymousTypePropertyReferenceToSelfArray;
			}
			internal class FindSourceDefinitionsResult {
				//public readonly NamedSymbolKind DefinitionKind;
				public readonly bool HasExternalVisibility;
				public readonly ReadOnlyCollection<string> DefinitionFiles;
				public FindSourceDefinitionsResult(IDECompilation compilation, SourceDefinitionOutputs outputs) {
					//DefinitionKind = outputs.definitionKind;
					HasExternalVisibility = outputs.hasExternalVisibility != 0;
					DefinitionFiles = new ReadOnlyCollection<string>(outputs.fileNames);
				}
			}
			internal class FindSourceDefinitionsAndDetermineSymbolResult : FindSourceDefinitionsResult {
				public readonly string RQName;
				public readonly string RQNameForParameterFromOtherPartialMethod;
				public readonly string AssemblyName;
				public readonly ReadOnlyCollection<string> NamespaceDefiningAssemblies;
				public readonly IList<bool> AnonymousTypePropertyReferenceToSelf;
				internal FindSourceDefinitionsAndDetermineSymbolResult(IDECompilation compilation, SourceDefinitionOutputs helper, SymbolInfoHolder symbolInfo) : base(compilation, helper) {
					RQName = symbolInfo.rqName;
					RQNameForParameterFromOtherPartialMethod = symbolInfo.RQNameForParameterFromOtherPartialMethod;
					AssemblyName = symbolInfo.assemblyName;

					if (symbolInfo.anonymousTypePropertyReferenceToSelfArray != null) {
						AnonymousTypePropertyReferenceToSelf =
							symbolInfo.anonymousTypePropertyReferenceToSelfArray.Select(i => i != 0).ToList();
					}
					NamespaceDefiningAssemblies = new ReadOnlyCollection<string>(symbolInfo.namespaceDefiningAssemblies);
				}
			}

			static Func<IDECompilation, SafeHandle> CompilationHandle = (Func<IDECompilation, SafeHandle>)Delegate.CreateDelegate(typeof(Func<IDECompilation, SafeHandle>), typeof(IDECompilation).GetProperty("SafeHandle", BindingFlags.Instance | BindingFlags.NonPublic).GetMethod);
			static Func<ParseTreeNode, IntPtr> ParseTreeNodePointer = (Func<ParseTreeNode, IntPtr>)Delegate.CreateDelegate(typeof(Func<ParseTreeNode, IntPtr>), typeof(ParseTreeNode).GetProperty("Pointer", BindingFlags.Instance | BindingFlags.NonPublic).GetMethod);

			// Stolen from Microsoft.VisualStudio.CSharp.Services.Language.Refactoring.RefactoringInterop
			internal static FindSourceDefinitionsAndDetermineSymbolResult FindSourceDefinitionsAndDetermineSymbolFromParseTree(IDECompilation compilation, IRefactorProgressUI progressUI, ParseTreeNode parseTreeNode) {
				SourceDefinitionOutputs sourceDefinitionOutputs = new SourceDefinitionOutputs();
				SymbolInfoHolder symbolInfoHolder = new SymbolInfoHolder();

				ParseTree.Handle anonymousTypePropertyRefOwnerHandle;
				NamedSymbolKind definitionKind;

				Refactoring_FindSourceDefinitionsAndDetermineSymbolFromParseTree(
					CompilationHandle(compilation),
					progressUI,
					ParseTreeNodePointer(parseTreeNode),
					out definitionKind,
					out symbolInfoHolder.rqName,
					out symbolInfoHolder.RQNameForParameterFromOtherPartialMethod,
					out symbolInfoHolder.assemblyName,
					out symbolInfoHolder.namespaceDefiningAssemblies,
					out anonymousTypePropertyRefOwnerHandle,
					out symbolInfoHolder.anonymousTypePropertyRefNodePointer,
					out sourceDefinitionOutputs.hasExternalVisibility,
					out sourceDefinitionOutputs.fileNames,
					out sourceDefinitionOutputs.nodeOwners,
					out sourceDefinitionOutputs.nodePointers,
					out symbolInfoHolder.anonymousTypePropertyReferenceToSelfArray
				);
				return new FindSourceDefinitionsAndDetermineSymbolResult(compilation, sourceDefinitionOutputs, symbolInfoHolder);
			}

			[DllImport("CSLangSvc.dll", PreserveSig = false)]
			internal static extern void Refactoring_FindSourceDefinitionsAndDetermineSymbolFromParseTree(
				SafeHandle compilationScope,
				IRefactorProgressUI progressUI,
				IntPtr refNodePointer,
				out NamedSymbolKind definitionKind,
				[MarshalAs(UnmanagedType.BStr)] out string rqName,
				[MarshalAs(UnmanagedType.BStr)] out string RQNameForParameterFromOtherPartialMethod,
				[MarshalAs(UnmanagedType.BStr)] out string assemblyName,
				[MarshalAs(UnmanagedType.SafeArray)] out string[] namespaceDefiningAssemblies,
				out ParseTree.Handle anonymousTypePropertyRefOwner,
				out IntPtr anonymousTypePropertyRefPointer,
				out int hasExternalVisibility,
				[MarshalAs(UnmanagedType.SafeArray)] out string[] sourceLocationFilenames,
				[MarshalAs(UnmanagedType.SafeArray)] out IntPtr[] sourceLocationOwners,
				[MarshalAs(UnmanagedType.SafeArray)] out IntPtr[] sourceLocationNodePointers,
				[MarshalAs(UnmanagedType.SafeArray)] out int[] anonymousTypePropertyReferenceToSelf
			);
		}
	}
}
