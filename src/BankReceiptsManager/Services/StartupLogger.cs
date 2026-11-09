using System.IO;

namespace BankReceiptsManager.Services;

public static class StartupLogger
{
    private static readonly string LogFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BankReceiptsManager", "Logs");

    public static void Log(string stage, string? message = null, Exception? ex = null)
    {
        try
        {
            Directory.CreateDirectory(LogFolder);
            var file = Path.Combine(LogFolder, $"startup-{DateTime.Today:yyyy-MM-dd}.log");
            var line = $"{DateTime.Now:O} | {Environment.UserName} | {stage}";
            if (!string.IsNullOrEmpty(message)) line += $" | {message}";
            if (ex != null) line += $"\n{ex}";
            File.AppendAllText(file, line + Environment.NewLine);
        }
        catch
        {
            // Never let logging crash startup
        }
    }
}
