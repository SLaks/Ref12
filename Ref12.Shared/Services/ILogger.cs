using System;

namespace SLaks.Ref12.Services {
	public interface ILogger {
		void Log(string message);
		void Log(string message, Exception ex);
	}
}
