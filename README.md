#Ref12

This Visual Studio extension intercepts the Go To Definition command (F12) and forwards to the new [.Net Reference Source browser](http://referencesource-beta.microsoft.com/).
Simply [install the extension](http://visualstudiogallery.msdn.microsoft.com/f89b27c5-7d7b-4059-adde-7ccc709fa86e) from the gallery on Visual Studio 2010 or later, place the cursor on any class or member in the .Net framework, and press F12 to see beautiful, hyperlinked source code, complete with original comments.  (thanks to [Kirill Osenkov](https://twitter.com/KirillOsenkov) for his amazing work on this site)


This extension uses undocumentd native C# langauge services APIs to find the [RQName](http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.shell.interop.ivsrefactornotify.aspx#remarksToggle "Refactor-Qualified Name") of the identifier under the cursor, checks if it's defined in an assembly included in the Reference Source ([list](http://referencesource-beta.microsoft.com/assemblies.txt)), and opens the correct URL in a browser.

The extension does not function at all in VB source files (pressing F12 from a C# source file to [VB code](http://referencesource-beta.microsoft.com/#Microsoft.VisualBasic/)) works fine).  Unlike Roslyn, the native language services are completely different for C# & VB; adding VB support would require separate code using the undocumentd native VB langauge services APIs to find the symbol under the cursor.

It uses MEF to import `IReferenceSourceProvider` instances that can navigate to members in specific assemblies; you can export implementations of this interface to add other external source providers.

![F12 to .Net Reference Source](http://i1.visualstudiogallery.msdn.s-msft.com/f89b27c5-7d7b-4059-adde-7ccc709fa86e/image/file/125181/1/ref12%20screenshot.png)