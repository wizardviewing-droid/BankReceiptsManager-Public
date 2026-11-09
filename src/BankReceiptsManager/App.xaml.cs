using System.Windows;
using System.Windows.Threading;

namespace BankReceiptsManager;

public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        Services.StartupLogger.Log("OnStartup", "Application starting");
        base.OnStartup(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Services.StartupLogger.Log("DispatcherUnhandledException", e.Exception.Message, e.Exception);
        MessageBox.Show(
            $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nDetails have been written to the local log.",
            "Bank Receipts Manager",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            Services.StartupLogger.Log("DomainUnhandledException", ex.Message, ex);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Services.StartupLogger.Log("UnobservedTaskException", e.Exception.Message, e.Exception);
        e.SetObserved();
    }
}
