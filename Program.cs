using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Phidgets2Prosim
{
	internal static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			FileLogger.Init();

			// Catches exceptions raised on the UI thread's message loop,
			// including exceptions rethrown from "async void" methods whose
			// continuation was captured on this thread's SynchronizationContext
			// (several Open() methods in this codebase are "async void").
			Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
			Application.ThreadException += (s, e) =>
			{
				FileLogger.LogError("UI THREAD EXCEPTION: " + e.Exception);
			};

			// Last-resort catch-all for exceptions on ANY thread that would
			// otherwise crash the process with no trace at all. By the time
			// this fires the process is usually already going down - this is
			// purely about getting the exception written to disk before that
			// happens.
			AppDomain.CurrentDomain.UnhandledException += (s, e) =>
			{
				FileLogger.LogError($"UNHANDLED EXCEPTION (terminating: {e.IsTerminating}): " + e.ExceptionObject);
				FileLogger.LogSessionEnd(false, "AppDomain.UnhandledException");
			};

			// Fire-and-forget async Tasks (the "_ = SomeAsyncMethod();" pattern
			// used throughout - e.g. FireReleaseAsync) that fault without ever
			// being awaited raise this instead of failing silently. Mark
			// observed so it doesn't also trigger process termination.
			TaskScheduler.UnobservedTaskException += (s, e) =>
			{
				FileLogger.LogError("UNOBSERVED TASK EXCEPTION: " + e.Exception);
				e.SetObserved();
			};

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			try
			{
				Application.Run(new Form1());
				FileLogger.LogSessionEnd(true);
			}
			catch (Exception ex)
			{
				FileLogger.LogError("FATAL EXCEPTION escaped Application.Run: " + ex);
				FileLogger.LogSessionEnd(false, "Exception escaped Application.Run");
				throw;
			}
			finally
			{
				FileLogger.Shutdown();
			}
		}
	}
}