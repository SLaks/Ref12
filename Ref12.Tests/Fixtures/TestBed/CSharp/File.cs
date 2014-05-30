using System;
using System.Data;
using System.Linq;
using System.Threading;
using Microsoft.Win32;

namespace CSharp {
	public class File {
		public void MyMethod() {
			var path = Environment.GetFolderPath(Environment.SpecialFolder.CommonOemLinks);
			SystemEvents.PowerModeChanged += (s, e) => e.Mode.ToString();
			"".Aggregate(new Exception(), (e, c) => new Exception(e.Message + c));
			System.IO.Log.LogStore x;

			int y;
			// M:System.Int32.TryParse(System.String,System.Int32@)
			int.TryParse("a", out y);
			// M:System.Threading.Interlocked.Add(System.Int32@,System.Int32)
			Interlocked.Add(ref y, 2);
		}
		class A<T> {
			class B<U> {
				// M:CSharp.File.A`1.B`1.M``1(`0,`1,`0,``0)
				void M<V>(T x, U a, T b, V c) {
					M(x, a, b, c);
				}
			}
		}
	}
}
