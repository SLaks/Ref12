# Ref12

This Visual Studio extension intercepts the Go To Definition command (F12) and forwards to the new [.Net Reference Source Browser](http://referencesource-beta.microsoft.com/).
Simply [install the extension](http://visualstudiogallery.msdn.microsoft.com/f89b27c5-7d7b-4059-adde-7ccc709fa86e) from the gallery on Visual Studio 2010 or later, place the cursor on any class or member in the .Net framework, and press F12 to see beautiful, hyperlinked source code, complete with original comments  (thanks to [Kirill Osenkov](https://twitter.com/KirillOsenkov) for his amazing work on this web app).
No more "From Metadata" tabs! 

**Now with VB support!**

_VB support may be less reliable_; I needed to delve more deeply into the undocumented syntax tree APIs and may have missed some cases.  
If you notice that F12 isn't working when it should or if it jumps to the wrong method, please file a bug, and include the full line of VB source that you pressed F12 on.

## Known Issues

 - The reference source code does not support links to overloaded operators, so F12 on operators is not handled.
 - C#: F12 in Metadata as Source tabs (from assemblies without source) does not work on VS2012 or 2010.  I can't find any way to get the compilation for these tabs from the language service.
 - VB: Type inference on lambda expression parameters does not work; inferred parameters will be incorrectly resolved to `Object`.  This muight be solvable by binding the parent SyntaxNode and walking down the bound tree.
 - VB: F12 on attribute properties does not work.  I cannot find any way to resolve them to symbols.
 - VB: F12 on `As New` constructor references does not work.

## Implementation
This extension uses undocumented APIs from the native C# language services to find the [RQName](http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.shell.interop.ivsrefactornotify.aspx#remarksToggle "Refactor-Qualified Name") of the identifier under the cursor, checks if it's defined in an assembly included in the Reference Source ([list](http://referencesource-beta.microsoft.com/assemblies.txt)), and opens the correct URL in a browser.

It uses MEF to import `IReferenceSourceProvider` instances that can navigate to members in specific assemblies; you can export implementations of this interface to add other external source providers.

To allow classes to inherit types in unversioned assemblies, the native VB implementation is in a separate Ref12.Unversioned assembly, so that I can add my AssemblyResolve handler without MEF trying to load those types.

Because Roslyn references newer versions of the VS editor assemblies, the Roslyn `ISymbolResolver` implementation lives in a separate assembly, to avoid compiler version conflicts.  This assembly is loaded at runtime if Roslyn is detected.  This also includes a Roslyn DLL file which is not (yet?) available on NuGet, but which is required to get a `Document` from a TextBuffer.

**Pull requests welcome**

![F12 to .Net Reference Source](http://i1.visualstudiogallery.msdn.s-msft.com/f89b27c5-7d7b-4059-adde-7ccc709fa86e/image/file/125181/1/ref12%20screenshot.png)

# License
[MIT](http://opensource.org/licenses/MIT)
