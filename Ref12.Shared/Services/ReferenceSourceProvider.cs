using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Task = System.Threading.Tasks.Task;


namespace SLaks.Ref12.Services {
	public interface IReferenceSourceProvider {
		ISet<string> AvailableAssemblies { get; }
		void Navigate(SymbolInfo symbol);
	}
	[Export(typeof(IReferenceSourceProvider))]
	public class RoslynReferenceSourceProvider : ReferenceSourceProvider {
		[ImportingConstructor]
		public RoslynReferenceSourceProvider(ILogger logger) : base(logger, "http://index", "https://sourceroslyn.io") {
		}
	}
	[Export(typeof(IReferenceSourceProvider))]
	public class DotNetReferenceSourceProvider : ReferenceSourceProvider {
		[ImportingConstructor]
		public DotNetReferenceSourceProvider(ILogger logger) : base(logger, "http://index", "https://referencesource.microsoft.com") {
		}
	}

	public class ReferenceSourceProvider : IReferenceSourceProvider, IDisposable {
		IEnumerable<string> urls;

		readonly ILogger logger;
		readonly Timer timer;

		public ReferenceSourceProvider(ILogger logger, params string[] urls) {
			this.logger = logger;
			this.urls = urls;
			timer = new Timer(async _ => await LookupService(), null, 0, (int)TimeSpan.FromMinutes(60).TotalMilliseconds);
			AvailableAssemblies = new HashSet<string>();

			NetworkChange.NetworkAvailabilityChanged += (s, e) => {
				if (e.IsAvailable)
					LookupService().ToString();	// Fire and forget
				else
					AvailableAssemblies = new HashSet<string>();
			};
			NetworkChange.NetworkAddressChanged += async (s, e) => await LookupService();
		}


		string baseUrl;

		public ISet<string> AvailableAssemblies { get; private set; }

		public async Task LookupService() {
			Exception lastFailure = new Exception("No reference source URLs defined");
			foreach (var url in urls) {
				string assemblyList;
				try {
					using (var handler = new HttpClientHandler()) {
						handler.Proxy = WebRequest.GetSystemWebProxy();
						if (handler.Proxy != null) {
							handler.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;
						}

						using (var http = new HttpClient(handler, false))
							assemblyList = await http.GetStringAsync(url + "/assemblies.txt");
					}

					// Format:
					// AssemblyName; ProjectIndex; DependentAssemblies
					var assemblies = new HashSet<string>(
						assemblyList.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
									.Where(s => !s.Contains(";-1;"))
									.Select(s => s.Remove(s.IndexOf(';')))
					);

					// If nothing changed, don't spam the log
					if (assemblies.SetEquals(this.AvailableAssemblies) && url == this.baseUrl)
						return;
					AvailableAssemblies = assemblies;
					baseUrl = url;
					logger.Log("Using reference source from " + url + " with " + AvailableAssemblies.Count + " assemblies");
					return;
				} catch (Exception ex) {
					lastFailure = ex;
					continue;
				}
			}
			logger.Log("Errors occurred while trying all reference URLs; Ref12 will not work", lastFailure);
			AvailableAssemblies = new HashSet<string>();
		}

		public void Navigate(SymbolInfo symbol) {
			var url = baseUrl + "/" + symbol.AssemblyName + "/a.html#" + GetHash(symbol.IndexId);

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
