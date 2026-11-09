using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;

namespace BankReceiptsManager.ViewModels;

public partial class ImportViewModel : ObservableObject
{
    private readonly MainViewModel _main;

    [ObservableProperty] private string selectedFilePath = string.Empty;
    [ObservableProperty] private int importedCount;
    [ObservableProperty] private decimal importedTotal;
    [ObservableProperty] private string importMessage = "Select a Barclays BACS CHAPS RECEIPTS .xls file";

    public ObservableCollection<BankReceipt> ImportedReceipts { get; } = new();

    public ImportViewModel(MainViewModel main)
    {
        _main = main;
    }

    [RelayCommand]
    private void BrowseFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Excel files (*.xls;*.xlsx)|*.xls;*.xlsx|All files (*.*)|*.*",
            Title = "Select Barclays BACS CHAPS RECEIPTS export"
        };

        if (dialog.ShowDialog() == true)
        {
            SelectedFilePath = dialog.FileName;
            ImportMessage = $"Selected: {Path.GetFileName(SelectedFilePath)}";
        }
    }

    [RelayCommand]
    private async Task ImportReceipts()
    {
        if (string.IsNullOrWhiteSpace(SelectedFilePath) || !File.Exists(SelectedFilePath))
        {
            ImportMessage = "Please select a valid Barclays export file.";
            return;
        }

        try
        {
            _main.IsProcessing = true;
            _main.SetStatus("Importing receipts...");

            var importer = new Services.BarclaysImportService();
            var receipts = await Task.Run(() => importer.Import(SelectedFilePath));

            ImportedReceipts.Clear();
            foreach (var r in receipts)
                ImportedReceipts.Add(r);

            ImportedCount = receipts.Count;
            ImportedTotal = receipts.Sum(r => r.ReceiptAmount);
            ImportMessage = $"Successfully imported {ImportedCount} receipts totalling {ImportedTotal:C2}.";

            // Copy to Review for next step
            _main.Review.LoadReceipts(receipts);

            _main.SetStatus("Import complete. Proceed to Review Allocations.");
        }
        catch (Exception ex)
        {
            ImportMessage = $"Import failed: {ex.Message}";
            _main.SetStatus("Import failed");
        }
        finally
        {
            _main.IsProcessing = false;
        }
    }
}
