Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.Linq
Imports System.Xml.Linq
Imports Microsoft.Win32

Public Class File
	'T:System.Lazy`1
	Inherits Lazy(Of Exception)
	Sub MyMethod()
		'M:System.Int32.TryParse(System.String,System.Int32@)
		Integer.TryParse(1, 2)

		'M:System.Environment.SetEnvironmentVariable(System.String,System.String,System.EnvironmentVariableTarget)
		Environment.SetEnvironmentVariable("a", "b", EnvironmentVariableTarget.Process)

		'E:Microsoft.Win32.SystemEvents.PowerModeChanged
		AddHandler SystemEvents.PowerModeChanged, Sub(s, e) e.Mode.ToString()

		Dim str = ""

		'M:System.Linq.Enumerable.Aggregate``2(System.Collections.Generic.IEnumerable{``0},``1,System.Func{``1,``0,``1})
		str.Aggregate(New Exception(), Function(e, c) New Exception(e.Message + c))

		'M:System.Collections.Generic.List`1.ctor(System.Collections.Generic.IEnumerable{`0})
		'T:System.Func`2	
		Dim o As New List(Of Func(Of Int32, DateTime))(collection:=Nothing)

		'M:System.Collections.Generic.List`1.ConvertAll``1(System.Converter{`0,``0})
		'M:System.Func`2.Invoke(`0)
		o.ConvertAll(Function(d) d.Invoke(2).ToString)
		'P:System.Collections.Generic.List`1.Item(System.Int32)
		o.Item(1) = Nothing

		'P:System.Collections.Generic.List`1.Enumerator.Current
		o.GetEnumerator().Current.ToString()

		Dim iStr, iInt As KeyedCollection(Of String, Date)
		'P:System.Collections.ObjectModel.KeyedCollection`2.Item(`0)
		'M:System.Int32.ToString
		iStr.Item(0.ToString).ToBinary()
		'P:System.Collections.ObjectModel.Collection`1.Item(System.Int32)
		iInt.Item(0).ToFileTime()

		Dim ns As XNamespace = "http://slaks.net"
		'M:System.Xml.Linq.XNamespace.op_Addition(System.Xml.Linq.XNamespace,System.String)
		Dim n = ns + "a"
	End Sub
	Class A(Of T, W)
		Class B(Of U, X)
			'M:VBClassLibrary.Class1.A`2.B`2.M``1(`1,`0,``0)
			Sub M(Of V)(a As U, b As T, c As V)

			End Sub
		End Class
	End Class

End Class
