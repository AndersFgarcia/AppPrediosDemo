using System.Windows;

public partial class App : Application
{
    public App()
    {
        this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        LogError("DispatcherUnhandledException", e.Exception);
        MessageBox.Show("Ocurrió un error no controlado. Revise el log o contacte al administrador.");
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            LogError("UnhandledException", ex);
    }

    private void LogError(string origen, Exception ex)
    {
        try
        {
            var path = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "errores.log");

            System.IO.File.AppendAllText(
                path,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {origen}: {ex}\r\n");
        }
        catch
        {
            // no lanzar nada desde aquí
        }
    }
}
