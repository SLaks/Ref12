using System.IO;
using System.Threading.Tasks;
using System.Windows;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VSSDK.Tools.VsIdeTesting;
using SLaks.Ref12.Services;

namespace Ref12.Tests {
	[TestClass]
	public class VBTests {
		public static DTE DTE { get { return VsIdeTestHostContext.Dte; } }

		// Hosted tests run out of a TestResults directory in the solution path.
		static readonly string SolutionDir = Path.Combine(Path.GetDirectoryName(typeof(IntegrationTests).Assembly.Location), @"..\..\..\Ref12.Tests\Fixtures\TestBed");


		private static IComponentModel componentModel;
		private static ITextView textView;
		private static string fileName;

		[ClassInitialize]
		public static void PrepareSolution(TestContext context) {
			DTE.Solution.Open(Path.Combine(SolutionDir, "TestBed.sln"));

			componentModel = (IComponentModel)VsIdeTestHostContext.ServiceProvider.GetService(typeof(SComponentModel));
			fileName = Path.GetFullPath(Path.Combine(SolutionDir, "Basic", "File.vb"));
			DTE.ItemOperations.OpenFile(fileName).Activate();
			textView = GetCurentTextView();
		}

		[TestMethod]
		[HostType("VS IDE")]
		public async Task VBResolverTypeTests() {
			// Hop on to the UI thread so the language service APIs work
			await Application.Current.Dispatcher.NextFrame();

			var symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("Inherits Lazy").End);
			Assert.AreEqual("mscorlib", symbol.AssemblyName);
			Assert.AreEqual("T:System.Lazy`1", symbol.IndexId);

			symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("AddHandler Microsoft.Win32").End);
			Assert.IsNull(symbol, "Namespaces should not be resolved");

			symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("New List(Of Func").End);
			Assert.AreEqual("T:System.Func`2", symbol.IndexId);
		}

		[TestMethod]
		[HostType("VS IDE")]
		public async Task VBResolverAttributeTests() {
			// Hop on to the UI thread so the language service APIs work
			await Application.Current.Dispatcher.NextFrame();

			var symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("<AttributeUsage").End);
			Assert.AreEqual("mscorlib", symbol.AssemblyName);
			Assert.AreEqual("T:System.AttributeUsageAttribute", symbol.IndexId);

			symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("AttributeUsage(AttributeTargets.All").End);
			Assert.AreEqual("mscorlib", symbol.AssemblyName);
			Assert.AreEqual("F:System.AttributeTargets.All", symbol.IndexId);

			// I can't find any way to resolve attribute properties to their symbols.
			//symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("AllowMultiple").End);
			//Assert.AreEqual("P:System.AttributeUsageAttribute.AllowMultiple", symbol.IndexId);
		}

		[TestMethod]
		[HostType("VS IDE")]
		public async Task VBResolverMemberTests() {
			// Hop on to the UI thread so the language service APIs work
			await Application.Current.Dispatcher.NextFrame();

			var symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("Environment.SetEnvironmentVariable(\"").End - 4);
			Assert.IsFalse(symbol.HasLocalSource);
			Assert.AreEqual("mscorlib", symbol.AssemblyName);
			Assert.AreEqual("M:System.Environment.SetEnvironmentVariable(System.String,System.String,System.EnvironmentVariableTarget)", symbol.IndexId);

			symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("EnvironmentVariableTarget.Process)").End - 1);
			Assert.AreEqual("mscorlib", symbol.AssemblyName);
			Assert.AreEqual("F:System.EnvironmentVariableTarget.Process", symbol.IndexId);

			symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("AddHandler Microsoft.Win32.SystemEvents.PowerModeChanged").End);
			Assert.IsFalse(symbol.HasLocalSource);
			Assert.AreEqual("System", symbol.AssemblyName);
			Assert.AreEqual("E:Microsoft.Win32.SystemEvents.PowerModeChanged", symbol.IndexId);

			symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("ex = New Exception").End);
			Assert.AreEqual("M:System.Exception.ctor", symbol.IndexId);

			symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("0.ToString").End);
			Assert.AreEqual("M:System.Int32.ToString", symbol.IndexId);

			// TODO: Wait for the reference source to support operators.
			//symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("ns +").End);
			//Assert.AreEqual("M:System.Xml.Linq.XNamespace.op_Addition(System.Xml.Linq.XNamespace,System.String)", symbol.IndexId);
		}

		[TestMethod]
		[HostType("VS IDE")]
		public async Task VBResolverParameterTests() {
			// Hop on to the UI thread so the language service APIs work
			await Application.Current.Dispatcher.NextFrame();

			var symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("e.Message + c").End);
			Assert.IsNull(symbol, "Lambda parameters should not be resolved");

			Assert.Inconclusive("VB cannot resolve inferred lambda parameter types?");
			System.Diagnostics.Debugger.Launch();

			symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("Sub(myLL").End);
			Assert.AreEqual("mscorlib", symbol.AssemblyName);
			Assert.AreEqual("T:System.Collections.Generic.LinkedList`1", symbol.IndexId, "Lambda parameter declarations should resolve to open generic types");

			symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("Function(e").End);
			Assert.AreEqual("mscorlib", symbol.AssemblyName);
			Assert.AreEqual("T:System.Exception", symbol.IndexId, "Lambda parameter declarations should resolve to their inferred types");
		}

		[TestMethod]
		[HostType("VS IDE")]
		public async Task VBResolverGenericsTests() {
			// Hop on to the UI thread so the language service APIs work
			await Application.Current.Dispatcher.NextFrame();

			var symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("str.Aggregate").End);
			Assert.IsFalse(symbol.HasLocalSource);
			Assert.AreEqual("System.Core", symbol.AssemblyName);
			Assert.AreEqual("M:System.Linq.Enumerable.Aggregate``2(System.Collections.Generic.IEnumerable{``0},``1,System.Func{``1,``0,``1})", symbol.IndexId);

			symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("o = New List").End);
			Assert.AreEqual("M:System.Collections.Generic.List`1.ctor(System.Collections.Generic.IEnumerable{`0})", symbol.IndexId);

			symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("New System.Collections.Gene").End);
			Assert.AreEqual("M:System.Collections.Generic.List`1.ctor(System.Collections.Generic.IEnumerable{`0})", symbol.IndexId);
			symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("New System.Collections.Generic.List").End);
			Assert.AreEqual("M:System.Collections.Generic.List`1.ctor(System.Collections.Generic.IEnumerable{`0})", symbol.IndexId);

			symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("o.ConvertAll").End);
			Assert.AreEqual("M:System.Collections.Generic.List`1.ConvertAll``1(System.Converter{`0,``0})", symbol.IndexId);

			symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("d.Invoke").End);
			Assert.AreEqual("M:System.Func`2.Invoke(`0)", symbol.IndexId);

			symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("o.Item").End);
			Assert.AreEqual("P:System.Collections.Generic.List`1.Item(System.Int32)", symbol.IndexId);

			symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("GetEnumerator().Current").End);
			Assert.AreEqual("P:System.Collections.Generic.List`1.Enumerator.Current", symbol.IndexId);

			symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("iStr.Item").End);
			Assert.AreEqual("P:System.Collections.ObjectModel.KeyedCollection`2.Item(`0)", symbol.IndexId);

			symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("iInt.Item").End);
			Assert.AreEqual("P:System.Collections.ObjectModel.Collection`1.Item(System.Int32)", symbol.IndexId);

			symbol = new VBResolver().GetSymbolAt(fileName, textView.FindSpan("Call M(").End - 1);
			Assert.IsTrue(symbol.HasLocalSource);
			Assert.AreEqual("M:Basic.File.A`2.B`2.M``1(`2,`0,``0)", symbol.IndexId);
		}

		///<summary>Gets the TextView for the active document.</summary>
		public static ITextView GetCurentTextView() {
			var editorAdapter = componentModel.GetService<IVsEditorAdaptersFactoryService>();

			return editorAdapter.GetWpfTextView(GetCurrentNativeTextView());
		}
		public static IVsTextView GetCurrentNativeTextView() {
			var textManager = (IVsTextManager)VsIdeTestHostContext.ServiceProvider.GetService(typeof(SVsTextManager));

			IVsTextView activeView = null;
			ErrorHandler.ThrowOnFailure(textManager.GetActiveView(1, null, out activeView));
			return activeView;
		}
	}
}
