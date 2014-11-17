using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.Text;

namespace SLaks.Ref12 {
	///<summary>Redirects loads of Roslyn assemblies to the version in the current VS instance.</summary>
	public static partial class RoslynAssemblyRedirector {
		public static void Register() { }		// Force static initializer to run

		static readonly Version roslynVersion;
		static readonly byte[] publicKeyToken;

		static RoslynAssemblyRedirector() {
			try {
				var roslynAssembly = Assembly.Load("Microsoft.CodeAnalysis");
				roslynVersion = roslynAssembly.GetName().Version;
				publicKeyToken = roslynAssembly.GetName().GetPublicKeyToken();
			} catch {
				return;	// No version of Roslyn is installed
			}

			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
		}


		const string prefix = "Microsoft.CodeAnalysis";
		static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
			if (!args.Name.StartsWith(prefix))
				return null;

			var name = new AssemblyName(args.Name);
			if (name.Version == roslynVersion)
				return null;
			name.SetPublicKeyToken(publicKeyToken);
			name.Version = roslynVersion;
			Debug.WriteLine("Ref12: Redirecting load of " + args.Name + ",\tfrom " + (args.RequestingAssembly == null ? "(unknown)" : args.RequestingAssembly.FullName));

			return Assembly.Load(name);
		}
	}
}
