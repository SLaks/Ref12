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
}
