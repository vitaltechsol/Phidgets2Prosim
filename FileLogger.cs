using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Phidgets2Prosim
{
	/// <summary>
	/// Simple, thread-safe, always-flushed file logger. Writes one file per
	/// session to a Logs folder next to the executable, keeping only the most
	/// recent MaxSessionLogFiles sessions so the folder can't grow without
	/// bound. Every write is flushed immediately: if the process dies hard,
	/// whatever was logged right up to that point should still be on disk,
	/// at the cost of the small overhead of flushing per line.
	///
	/// Note: this can only catch MANAGED exceptions. A native fault inside a
	/// driver (e.g. Phidget22's underlying C library) can kill the process
	/// before any .NET handler runs at all - in that case the log will just
	/// stop, with no final error line. That's still useful signal (the last
	/// thing logged is whatever was happening right before), just not the
	/// same as getting an exception message.
	/// </summary>
	public static class FileLogger
	{
		private static readonly object _lock = new object();
		private static StreamWriter _writer;
		private static string _logFilePath;
		private static bool _initialized = false;

		// Keep only the most recent N session log files (including the current
		// one) - older ones are deleted on startup so the Logs folder can't
		// grow without bound.
		private const int MaxSessionLogFiles = 5;

		public static string LogFilePath => _logFilePath;

		public static void Init()
		{
			lock (_lock)
			{
				if (_initialized) return;

				try
				{
					string baseDir = AppDomain.CurrentDomain.BaseDirectory;
					string logsDir = Path.Combine(baseDir, "Logs");
					Directory.CreateDirectory(logsDir);

					// One file per session (not per day) - makes it trivial to
					// grab exactly the file for the run that crashed, and makes
					// "keep the last N" a simple file-count trim rather than
					// needing to split a shared file by session boundary.
					string fileName = $"Phidgets2Prosim_{DateTime.Now:yyyy-MM-dd_HHmmss}.log";
					_logFilePath = Path.Combine(logsDir, fileName);

					// FileShare.ReadWrite so the file can still be opened/tailed
					// by another program (Notepad++, tail, etc.) while this app
					// holds it open.
					var fs = new FileStream(_logFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
					_writer = new StreamWriter(fs, Encoding.UTF8) { AutoFlush = true };

					_initialized = true;

					WriteRawLocked(
						$"=== SESSION STARTED {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ===" + Environment.NewLine +
						$"=== OS: {Environment.OSVersion} | CLR: {Environment.Version} | 64-bit process: {Environment.Is64BitProcess} ===" + Environment.NewLine +
						"================================================================"
					);

					TrimOldSessionLogs(logsDir);
				}
				catch
				{
					// If file logging itself can't start (e.g. no write permission
					// to the exe's folder), fail silent rather than take the whole
					// app down over a logging problem. There's nowhere useful left
					// to report this failure to.
					_initialized = false;
				}
			}
		}

		private static void TrimOldSessionLogs(string logsDir)
		{
			try
			{
				var files = new DirectoryInfo(logsDir)
					.GetFiles("Phidgets2Prosim_*.log")
					.OrderByDescending(f => f.LastWriteTimeUtc)
					.ToList();

				// Keep the newest MaxSessionLogFiles (which includes the one this
				// session just created); delete anything past that.
				foreach (var old in files.Skip(MaxSessionLogFiles))
				{
					try { old.Delete(); }
					catch { /* best-effort - e.g. a file still open elsewhere just stays until next run */ }
				}
			}
			catch
			{
				// Cleanup failing shouldn't prevent logging itself from working.
			}
		}

		public static void LogInfo(string message) => Write("INFO", message);
		public static void LogError(string message) => Write("ERROR", message);

		public static void LogSessionEnd(bool normal, string reason = null)
		{
			string status = normal ? "NORMAL" : "ABNORMAL";
			WriteRawLocked(
				$"=== SESSION ENDED ({status}) {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} ===" +
				(reason != null ? " Reason: " + reason : "")
			);
		}

		public static void Shutdown()
		{
			lock (_lock)
			{
				try { _writer?.Flush(); _writer?.Dispose(); } catch { /* ignore */ }
				_writer = null;
				_initialized = false;
			}
		}

		private static void Write(string level, string message)
		{
			WriteRawLocked($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}");
		}

		private static void WriteRawLocked(string line)
		{
			lock (_lock)
			{
				if (!_initialized) Init();
				if (!_initialized || _writer == null) return; // init failed - nothing more we can do

				try
				{
					_writer.WriteLine(line);
				}
				catch
				{
					// Logging must never itself throw and take down the app it's
					// trying to diagnose.
				}
			}
		}
	}
}