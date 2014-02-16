using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.VisualStudio.Text;

namespace SLaks.Ref12 {
	///<summary>Redirects loads of unversioned VS assemblies to the version in the current VS instance.</summary>
	static class AssemblyRedirector {
		///<summary>Gets the list of unqualified assembly names to redirect loads for.</summary>
		public static readonly ISet<string> TargetNames = new HashSet<string>();

		static AssemblyRedirector() {
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
		}

		// Find the VS version from a type in a versioned assembly,
		// relying on VS' <bindingRedirect>s for the right version.
		static readonly Version vsVersion = typeof(ITextBuffer).Assembly.GetName().Version;
		static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args) {
			var name = new AssemblyName(args.Name);
			if (!TargetNames.Contains(name.Name))
				return null;
			name.Version = vsVersion;
			if (name.ToString() == args.Name)   // Prevent recursion
				return null;

			Debug.WriteLine("Ref12: Redirecting load of " + args.Name + ",\tfrom " + (args.RequestingAssembly == null ? "(unknown)" : args.RequestingAssembly.FullName));

			return Assembly.Load(name);
		}
	}
}
