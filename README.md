#Ref12

This Visual Studio extension intercepts the Go To Definition command (F12) and forwards to the new [.Net Reference Source Browser](http://referencesource-beta.microsoft.com/).
Simply [install the extension](http://visualstudiogallery.msdn.microsoft.com/f89b27c5-7d7b-4059-adde-7ccc709fa86e) from the gallery on Visual Studio 2010 or later, place the cursor on any class or member in the .Net framework, and press F12 to see beautiful, hyperlinked source code, complete with original comments  (thanks to [Kirill Osenkov](https://twitter.com/KirillOsenkov) for his amazing work on this web app).
No more "From Metadata" tabs! 


##Implementation
This extension uses undocumented APIs from the native C# language services to find the [RQName](http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.shell.interop.ivsrefactornotify.aspx#remarksToggle "Refactor-Qualified Name") of the identifier under the cursor, checks if it's defined in an assembly included in the Reference Source ([list](http://referencesource-beta.microsoft.com/assemblies.txt)), and opens the correct URL in a browser.

The extension does not function at all in VB source files (pressing F12 from a C# source file to [VB code](http://referencesource-beta.microsoft.com/#Microsoft.VisualBasic/)) works fine).  Unlike Roslyn, the native language services are completely different for C# & VB; adding VB support would require separate code using separate undocumented APIs from the native VB language services to find the symbol under the cursor (probably `Microsoft.VisualBasic.SourceFile.GetSymbolAtPosition()`).

It uses MEF to import `IReferenceSourceProvider` instances that can navigate to members in specific assemblies; you can export implementations of this interface to add other external source providers.

**Pull requests welcome**

![F12 to .Net Reference Source](http://i1.visualstudiogallery.msdn.s-msft.com/f89b27c5-7d7b-4059-adde-7ccc709fa86e/image/file/125181/1/ref12%20screenshot.png)

