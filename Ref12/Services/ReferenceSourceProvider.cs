using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Task = System.Threading.Tasks.Task;


namespace SLaks.Ref12.Services {
	public interface IReferenceSourceProvider {
		ISet<string> AvailableAssemblies { get; }
		void Navigate(string assemblyName, string rqName);
	}
	[Export(typeof(IReferenceSourceProvider))]
	public class ReferenceSourceProvider : IReferenceSourceProvider, IDisposable {
		static readonly string[] urls = { "http://index", "http://referencesource.microsoft.com", "http://referencesource-beta.microsoft.com" };

		readonly ILogger logger;
		readonly Timer timer;

		[ImportingConstructor]
		public ReferenceSourceProvider(ILogger logger) {
			this.logger = logger;
			timer = new Timer(_ => LookupService(), null, 0, (int)TimeSpan.FromMinutes(60).TotalMilliseconds);
			AvailableAssemblies = new HashSet<string>();

			NetworkChange.NetworkAvailabilityChanged += (s, e) => {
				if (e.IsAvailable)
					LookupService().ToString(); // Fire and forget
				else
					AvailableAssemblies = new HashSet<string>();
			};
			NetworkChange.NetworkAddressChanged += (s, e) => LookupService();
		}


		string baseUrl;

		public ISet<string> AvailableAssemblies { get; private set; }

		public async Task LookupService() {
			foreach (var url in urls) {
				string assemblyList;
				try {
					using (var http = new HttpClient(new HttpClientHandler { UseDefaultCredentials = true }))
						assemblyList = await http.GetStringAsync(url + "/assemblies.txt");
					// Format:
					// AssemblyName; ProjectIndex; DependentAssemblies
					baseUrl = url;
					AvailableAssemblies = new HashSet<string>(
						assemblyList.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
									.Select(s => s.Remove(s.IndexOf(';')))
					);
					logger.Log("Using reference source from " + url + " with " + AvailableAssemblies.Count + " assemblies");
					return;
				} catch (Exception ex) {
					logger.Log("An error occurred while trying reference URL " + url + "; skipping", ex);
					continue;
				}
			}
			AvailableAssemblies = new HashSet<string>();
		}

		public void Navigate(string assemblyName, string rqName) {
			var url = baseUrl + "/" + assemblyName + "/a.html#" + GetHash(RQNameTranslator.ToIndexId(rqName));

			Process.Start(url);
		}



		///<summary>Releases all resources used by the ReferenceSourceProvider.</summary>
		public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
		///<summary>Releases the unmanaged resources used by the ReferenceSourceProvider and optionally releases the managed resources.</summary>
		///<param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				timer.Dispose();
			}
		}

		public static string GetHash(string result) {
			result = GetMD5Hash(result, 16);
			return result;
		}
		static string GetMD5Hash(string input, int digits) {
			using (var md5 = MD5.Create()) {
				var bytes = Encoding.UTF8.GetBytes(input);
				var hashBytes = md5.ComputeHash(bytes);
				return ByteArrayToHexString(hashBytes, digits);
			}
		}
		static string ByteArrayToHexString(byte[] bytes, int digits = 0) {
			if (digits == 0) {
				digits = bytes.Length * 2;
			}

			char[] c = new char[digits];
			byte b;
			for (int i = 0; i < digits / 2; i++) {
				b = ((byte)(bytes[i] >> 4));
				c[i * 2] = (char)(b > 9 ? b + 87 : b + 0x30);
				b = ((byte)(bytes[i] & 0xF));
				c[i * 2 + 1] = (char)(b > 9 ? b + 87 : b + 0x30);
			}

			return new string(c);
		}
	}
}
