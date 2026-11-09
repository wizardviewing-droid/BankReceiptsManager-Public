using System.Text.Json;

namespace BankReceiptsManager.Services;

public class AppSettings
{
    public string ReportsFolder { get; set; } = @"Y:\accounts\All cases reports\";
    public string PiReportFileName { get; set; } = "PI All cases.csv";
    public string HdrReportFileName { get; set; } = "HDR All cases - Balance review.csv";
    public string ConvReportFileName { get; set; } = "All cases Conv.csv";
    public string MailInfoFileName { get; set; } = "Mailinfo.xlsx"; // or .xls – user can change
    public string StorageFolder { get; set; } = @"Y:\accounts\Receipts Manager\";
    public bool EmailSendingEnabled { get; set; } = false; // disabled for Milestone 1

    public string PiPath => Path.Combine(ReportsFolder, PiReportFileName);
    public string HdrPath => Path.Combine(ReportsFolder, HdrReportFileName);
    public string ConvPath => Path.Combine(ReportsFolder, ConvReportFileName);
    public string MailInfoPath => Path.Combine(ReportsFolder, MailInfoFileName);
}

public class SettingsService
{
    private readonly string _settingsPath;

    public SettingsService()
    {
        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BankReceiptsManager");
        Directory.CreateDirectory(appData);
        _settingsPath = Path.Combine(appData, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
            return new AppSettings();

        try
        {
            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }
}
