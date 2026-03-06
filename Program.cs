namespace PanicMode;

internal static class Program
{
    private const string MutexName = "PanicMode_SingleInstance_Mutex";

    [STAThread]
    private static void Main()
    {
        // Single-instance guard
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "PanicMode is already running.\nCheck your system tray.",
                "PanicMode", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}