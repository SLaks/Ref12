using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.Text;

namespace SLaks.Ref12 {
	///<summary>Redirects loads of unversioned VS reference assemblies to the version in the current VS instance.</summary>
	public static partial class AssemblyRedirector {
		static bool field;
		public static void Register() { field.ToString(); }	// Force static initializer to run
		static AssemblyRedirector() {
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
		}

		// Find the VS version from a type in a versioned assembly,
		// relying on VS' <bindingRedirect>s for the right version.
		static readonly Version vsVersion = typeof(ITextBuffer).Assembly.GetName().Version;
		static readonly byte[] publicKeyToken = typeof(ITextBuffer).Assembly.GetName().GetPublicKeyToken();

		const string prefix = "Reference.";
		static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
			if (!args.Name.StartsWith(prefix))
				return null;

			var name = new AssemblyName(args.Name.Substring(prefix.Length));
			if (name.GetPublicKeyToken() != null && name.GetPublicKeyToken().Length > 0)
				return null;
			name.SetPublicKeyToken(publicKeyToken);
			name.Version = vsVersion;
			Debug.WriteLine("Ref12: Redirecting load of " + args.Name + ",\tfrom " + (args.RequestingAssembly == null ? "(unknown)" : args.RequestingAssembly.FullName));

			return Assembly.Load(name);
		}
	}
}
