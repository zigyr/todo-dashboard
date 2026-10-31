using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace TodoDashboard;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) => WriteCrashLog(args.ExceptionObject as Exception);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteCrashLog(e.Exception);
        e.Handled = true;
    }

    private static void WriteCrashLog(Exception? error)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "crash.log");
            File.WriteAllText(path, error?.ToString() ?? "unknown error");
        }
        catch
        {
            // Never let logging hide the original failure.
        }
    }
}
