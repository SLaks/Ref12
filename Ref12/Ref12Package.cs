using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace SLaks.Ref12 {
	// These GUIDS and command IDs must match the VSCT.
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[Guid("7E85FEAF-1785-4BE8-8E0C-0B4C55A97851")]
	[PackageRegistration(UseManagedResourcesOnly = true)]
	class Ref12Package : Package {
	}

	[Guid("BD27207E-0A63-4C87-A111-D226F1C22EE3")]
	enum Ref12Command {
		GoToDefinitionNative = 0
	}
}
