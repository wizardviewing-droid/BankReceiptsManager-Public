using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BankReceiptsManager.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly Services.SettingsService _settingsService = new();

    [ObservableProperty] private string reportsFolder = @"Y:\accounts\All cases reports\";
    [ObservableProperty] private string piFileName = "PI All cases.csv";
    [ObservableProperty] private string hdrFileName = "HDR All cases - Balance review.csv";
    [ObservableProperty] private string convFileName = "All cases Conv.csv";
    [ObservableProperty] private string mailInfoFileName = "Mailinfo.xlsx";
    [ObservableProperty] private string storageFolder = @"Y:\accounts\Receipts Manager\";
    [ObservableProperty] private string conveyancingEmail = "team_conveyancing@true.co.uk";
    [ObservableProperty] private string saveMessage = "";
    [ObservableProperty] private string storageStatus = "";

    public SettingsViewModel()
    {
        Load();
    }

    [RelayCommand]
    private void Load()
    {
        var s = _settingsService.Load();
        ReportsFolder = s.ReportsFolder;
        PiFileName = s.PiReportFileName;
        HdrFileName = s.HdrReportFileName;
        ConvFileName = s.ConvReportFileName;
        MailInfoFileName = s.MailInfoFileName;
        StorageFolder = s.StorageFolder;
        if (s.DepartmentEmailOverrides.TryGetValue("Conveyancing", out var conv))
            ConveyancingEmail = conv;
        SaveMessage = "Settings loaded.";
    }

    [RelayCommand]
    private void BrowseStorage()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select shared storage folder for Receipts Manager"
        };
        if (dialog.ShowDialog() == true)
            StorageFolder = dialog.FolderName + "\\";
    }

    [RelayCommand]
    private void TestStorage()
    {
        try
        {
            var svc = new Services.ExcelStorageService(StorageFolder);
            StorageStatus = svc.TestConnection()
                ? "Connection successful. Folder is writable."
                : "Connection failed. Check permissions and network availability.";
        }
        catch (Exception ex)
        {
            StorageStatus = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Save()
    {
        var s = new Services.AppSettings
        {
            ReportsFolder = ReportsFolder.TrimEnd('\\') + "\\",
            PiReportFileName = PiFileName,
            HdrReportFileName = HdrFileName,
            ConvReportFileName = ConvFileName,
            MailInfoFileName = MailInfoFileName,
            StorageFolder = StorageFolder.TrimEnd('\\') + "\\",
            EmailSendingEnabled = false,
            DepartmentEmailOverrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Conveyancing"] = ConveyancingEmail.Trim()
            }
        };
        _settingsService.Save(s);
        SaveMessage = "Settings saved. All users must use this shared location.";
    }
}
