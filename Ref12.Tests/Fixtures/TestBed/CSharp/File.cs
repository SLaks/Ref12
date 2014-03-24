using System;
using System.Linq;
using Microsoft.Win32;

namespace CSharp {
	public class File {
		public void MyMethod() {
			var path = Environment.GetFolderPath(Environment.SpecialFolder.CommonOemLinks);
			SystemEvents.PowerModeChanged += (s, e) => e.Mode.ToString();
			"".Aggregate(new Exception(), (e, c) => new Exception(e.Message + c));
		}
	}
	class A<T> {
		class B<U> {
			// M:ClassLibrary1.A`1.B`1.M``1(`1,`0,``0)
			void M<V>(U a, T b, V c) { }
		}
	}

}
