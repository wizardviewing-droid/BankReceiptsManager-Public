using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace BankReceiptsManager.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly Services.SettingsService _settingsService = new();
    private readonly Services.BarclaysImportService _importer = new();
    private readonly Services.CaseLookupService _caseLookup = new();
    private readonly Services.MailInfoService _mailInfo = new();
    private readonly Services.ReferenceExtractionService _extractor = new();
    private readonly Services.DuplicateDetectionService _duplicates = new();

    [ObservableProperty] private string selectedFilePath = string.Empty;
    [ObservableProperty] private int totalImported;
    [ObservableProperty] private decimal totalValue;
    [ObservableProperty] private int matchedCount;
    [ObservableProperty] private int reviewCount;
    [ObservableProperty] private int unmatchedCount;
    [ObservableProperty] private string statusMessage = "Select a Barclays BACS CHAPS RECEIPTS file and click Import & Match.";
    [ObservableProperty] private string filter = "All";
    [ObservableProperty] private bool isBusy;

    public ObservableCollection<BankReceipt> Receipts { get; } = new();
    public ObservableCollection<BankReceipt> FilteredReceipts { get; } = new();
    public ObservableCollection<string> FileHandlers { get; } = new();

    private Dictionary<string, string> _mailLookup = new();
    private Services.AppSettings _settings = new();

    public DashboardViewModel(MainViewModel main)
    {
        _main = main;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-GB");
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-GB");
        _ = LoadExistingBatchAsync();
    }

    private async Task LoadExistingBatchAsync()
    {
        // Let MainViewModel construction and MainWindow finish before touching shared storage
        await Task.Yield();

        try
        {
            Services.StartupLogger.Log("LoadExistingBatch", "Starting restore");
            var settings = _settingsService.Load();

            if (string.IsNullOrWhiteSpace(settings.StorageFolder))
            {
                StatusMessage = "Shared storage not configured. Open Settings to set the folder before processing.";
                SaveStatus = "Storage not configured";
                Services.StartupLogger.Log("LoadExistingBatch", "Storage folder empty");
                return;
            }

            var storage = new Services.ExcelStorageService(settings.StorageFolder);
            // Shared-folder access runs off the UI thread so a missing or slow drive cannot hang the window
            if (!await Task.Run(() => storage.TestConnection()))
            {
                StatusMessage = "Shared storage unavailable. Processing is disabled until the folder is accessible. Settings remain available.";
                SaveStatus = "Storage unavailable";
                Services.StartupLogger.Log("LoadExistingBatch", "Storage unavailable – continuing");
                return;
            }

            try
            {
                _mailLookup = await Task.Run(() => _mailInfo.Load(settings.MailInfoPath));
                FileHandlers.Clear();
                foreach (var name in _mailInfo.GetHandlerNames(_mailLookup))
                    FileHandlers.Add(name);
            }
            catch (Exception ex)
            {
                Services.StartupLogger.Log("LoadMailinfo", "Mailinfo unavailable", ex);
                StatusMessage = "Mailinfo could not be loaded. Processing is limited until the file is available.";
            }

            var existing = await Task.Run(() => storage.LoadDailyWorkbook(DateTime.Today));
            if (existing.Count > 0)
            {
                Receipts.Clear();
                foreach (var r in existing)
                    Receipts.Add(r);
                UpdateCounts();
                ApplyFilter();
                StatusMessage = $"Restored {existing.Count} receipts from today's shared workbook.";
            }
            Services.StartupLogger.Log("LoadExistingBatch", "Restore complete");
        }
        catch (Exception ex)
        {
            Services.StartupLogger.Log("LoadExistingBatch", "Unhandled during restore", ex);
            StatusMessage = $"Startup restore skipped: {ex.Message}. The application is usable; processing is disabled until storage is available.";
            SaveStatus = "Restore failed";
        }
    }

    partial void OnFilterChanged(string value) => ApplyFilter();

    [ObservableProperty] private string saveStatus = "All changes saved";

    private async Task AutoSaveAsync()
    {
        SaveStatus = "Saving...";
        try
        {
            var settings = _settingsService.Load();
            var storage = new Services.ExcelStorageService(settings.StorageFolder);

            if (!storage.TestConnection())
            {
                SaveStatus = "Save failed – shared storage unavailable";
                StatusMessage = "Cannot save: shared storage is unavailable. Processing blocked.";
                return;
            }

            if (!storage.TryAcquireSessionLock(Environment.UserName))
            {
                SaveStatus = "Save failed – another user is processing";
                return;
            }

            try
            {
                var processingDate = DateTime.Today;
                await Task.Run(() =>
                {
                    storage.SaveDailyWorkbook(processingDate, Receipts);
                    storage.AppendToMasterHistory(Receipts);
                    var audit = new Services.AuditService(settings.StorageFolder);
                    audit.Log("BatchSave", "multiple", null, $"Saved {Receipts.Count} receipts");
                });
                SaveStatus = "All changes saved";
            }
            finally
            {
                storage.ReleaseSessionLock();
            }
        }
        catch (Exception ex)
        {
            SaveStatus = "Save failed";
            StatusMessage = $"Save failed: {ex.Message}. Changes are not committed.";
        }
    }

    [RelayCommand]
    private void BrowseFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Excel files (*.xls;*.xlsx)|*.xls;*.xlsx|All files (*.*)|*.*",
            Title = "Select Barclays BACS CHAPS RECEIPTS export"
        };
        if (dialog.ShowDialog() == true)
            SelectedFilePath = dialog.FileName;
    }

    [RelayCommand]
    private async Task ImportAndMatch()
    {
        if (string.IsNullOrWhiteSpace(SelectedFilePath) || !File.Exists(SelectedFilePath))
        {
            StatusMessage = "Please select a valid Barclays export file.";
            return;
        }

        try
        {
            IsBusy = true;
            var settings = _settingsService.Load();
            var storage = new Services.ExcelStorageService(settings.StorageFolder);
            if (!storage.TestConnection())
            {
                StatusMessage = "Shared storage unavailable. Import blocked.";
                return;
            }

            StatusMessage = "Importing and matching...";

            var receipts = await Task.Run(() => _importer.Import(SelectedFilePath));

            Receipts.Clear();
            foreach (var r in receipts)
                Receipts.Add(r);

            await RunMatchingInternal();

            StatusMessage = $"Imported {TotalImported} receipts totalling {TotalValue:C2} (GBP). Matching complete.";
            await AutoSaveAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ReRunMatching()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Re-running matching (manual values preserved)...";
            await RunMatchingInternal();
            StatusMessage = "Matching re-run complete. Manual references and file handlers preserved.";
            await AutoSaveAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Matching failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunMatchingInternal()
    {
        _settings = _settingsService.Load();

        var caseLookup = await Task.Run(() => _caseLookup.LoadAllCases(_settings.PiPath, _settings.HdrPath, _settings.ConvPath));
        _mailLookup = await Task.Run(() => _mailInfo.Load(_settings.MailInfoPath));

        FileHandlers.Clear();
        foreach (var name in _mailInfo.GetHandlerNames(_mailLookup))
            FileHandlers.Add(name);

        var storage = new Services.ExcelStorageService(_settings.StorageFolder);
        if (!storage.TestConnection())
        {
            StatusMessage = "Shared storage unavailable. Matching blocked.";
            return;
        }

        var history = await Task.Run(() => storage.LoadMasterHistory());
        _duplicates.DetectDuplicates(Receipts.ToList(), history);

        foreach (var receipt in Receipts.Where(r => !r.IsRemovedFromBatch))
        {
            if (receipt.IsDuplicate)
            {
                // Do not overwrite Fully/Partially Allocated statuses
                if (receipt.Status == ProcessingStatus.FullyAllocated || receipt.Status == ProcessingStatus.PartiallyAllocated)
                    continue;
                receipt.Status = ProcessingStatus.Reviewed;
                continue;
            }

            // Preserve manual reference if present
            string? referenceToUse = !string.IsNullOrWhiteSpace(receipt.ManualReference)
                ? receipt.ManualReference.Trim().ToUpperInvariant()
                : null;

            bool usedManual = false;
            if (referenceToUse == null)
            {
                var candidates = _extractor.ExtractCandidates(receipt.TransactionDetails, receipt.CustomerReference);
                var valid = candidates.Where(c => caseLookup.ContainsKey(c.ToUpperInvariant())).Distinct().ToList();

                if (valid.Count == 1)
                {
                    referenceToUse = valid[0].ToUpperInvariant();
                    receipt.ExtractedReference = referenceToUse;
                }
                else if (valid.Count > 1)
                {
                    receipt.ExtractedReference = string.Join(", ", valid);
                    receipt.Status = ProcessingStatus.RequiresReview;
                    receipt.MatchingNotes = "Multiple valid references found – enter Manual Reference or select File Handler";
                    continue;
                }
                else
                {
                    receipt.ExtractedReference = candidates.FirstOrDefault();
                    receipt.Status = ProcessingStatus.Unmatched;
                    receipt.MatchingNotes = candidates.Count == 0 ? "No 6-digit reference found" : "No candidate matched a case report";
                    continue;
                }
            }
            else
            {
                usedManual = true;
                receipt.ExtractedReference ??= referenceToUse;
            }

            if (string.IsNullOrWhiteSpace(referenceToUse))
                continue;

            var (dept, handler, notes) = _caseLookup.Lookup(referenceToUse, caseLookup);

            if (dept == null)
            {
                receipt.Status = ProcessingStatus.RequiresReview;
                receipt.MatchingNotes = notes ?? "Reference not found in departmental reports";
                receipt.AllocationSource = usedManual ? "Manual" : "Automatic";
                continue;
            }

            // Only overwrite file handler if not manually set
            if (string.IsNullOrWhiteSpace(receipt.FileHandler) || !receipt.IsManualAllocation)
            {
                receipt.FileHandler = handler;
            }

            var individualEmail = _mailInfo.LookupEmail(receipt.FileHandler, _mailLookup);
            receipt.EmailAddress = individualEmail;
            receipt.NotificationRecipient = _mailInfo.ResolveNotificationRecipient(dept, individualEmail, _settings);

            receipt.Department = dept;
            receipt.Status = ProcessingStatus.Matched;
            receipt.AllocationSource = usedManual ? "Manual" : "Automatic";
            receipt.MatchingNotes = usedManual
                ? $"Manual reference matched to {dept}"
                : $"Auto-matched to {dept}";
            receipt.IsManualAllocation = usedManual || receipt.IsManualAllocation;
        }

        UpdateCounts();
        ApplyFilter();
    }

    [RelayCommand]
    private async Task OnManualReferenceChanged(object? parameter)
    {
        if (parameter is not BankReceipt receipt) return;
        receipt.IsManualAllocation = true;
        receipt.AllocationSource = "Manual";
        await AutoSaveAsync();
    }

    [RelayCommand]
    private async Task OnFileHandlerChanged(BankReceipt? receipt)
    {
        if (receipt == null || string.IsNullOrWhiteSpace(receipt.FileHandler)) return;

        receipt.IsManualAllocation = true;
        receipt.EmailAddress = _mailInfo.LookupEmail(receipt.FileHandler, _mailLookup);
        receipt.NotificationRecipient = _mailInfo.ResolveNotificationRecipient(receipt.Department, receipt.EmailAddress, _settings);
        receipt.AllocationSource = "Manual";
        receipt.MatchingNotes = $"File handler manually set to {receipt.FileHandler}";
        if (receipt.Status == ProcessingStatus.Unmatched)
            receipt.Status = ProcessingStatus.RequiresReview;

        _ = AutoSaveAsync();
    }

    [RelayCommand]
    private void RemoveReceipt(object? parameter)
    {
        if (parameter is not BankReceipt receipt) return;

        var result = MessageBox.Show(
            "Are you sure you want to remove this receipt from the current processing batch?",
            "Confirm Removal",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        receipt.IsRemovedFromBatch = true;
        // Audit note (full audit log in later milestone)
        receipt.MatchingNotes = $"Removed from batch by {Environment.UserName} at {DateTime.Now:dd/MM/yyyy HH:mm}";

        UpdateCounts();
        ApplyFilter();
    }

    private void UpdateCounts()
    {
        var active = Receipts.Where(r => !r.IsRemovedFromBatch).ToList();
        TotalImported = active.Count;
        TotalValue = active.Sum(r => r.ReceiptAmount);
        MatchedCount = active.Count(r => r.Status == ProcessingStatus.Matched || r.Status == ProcessingStatus.FullyAllocated);
        ReviewCount = active.Count(r => r.Status == ProcessingStatus.RequiresReview || r.Status == ProcessingStatus.PartiallyAllocated);
        UnmatchedCount = active.Count(r => r.Status == ProcessingStatus.Unmatched);
    }

    private void ApplyFilter()
    {
        FilteredReceipts.Clear();
        var query = Receipts.Where(r => !r.IsRemovedFromBatch);

        query = Filter switch
        {
            "Matched" => query.Where(r => r.Status == ProcessingStatus.Matched || r.Status == ProcessingStatus.FullyAllocated),
            "Requires Review" => query.Where(r => r.Status == ProcessingStatus.RequiresReview || r.Status == ProcessingStatus.PartiallyAllocated),
            "Unmatched" => query.Where(r => r.Status == ProcessingStatus.Unmatched),
            _ => query
        };

        foreach (var r in query)
            FilteredReceipts.Add(r);
    }

    [RelayCommand]
    private async Task MatchSingle(object? parameter)
    {
        if (parameter is not BankReceipt receipt || string.IsNullOrWhiteSpace(receipt.ManualReference)) return;

        try
        {
            var caseLookup = _caseLookup.LoadAllCases(_settings.PiPath, _settings.HdrPath, _settings.ConvPath);
            var reference = receipt.ManualReference.Trim().ToUpperInvariant();

            var (dept, handler, notes) = _caseLookup.Lookup(reference, caseLookup);

            if (dept == null)
            {
                receipt.Status = ProcessingStatus.RequiresReview;
                receipt.MatchingNotes = notes ?? "Manual reference not found";
                receipt.IsManualAllocation = true;
                receipt.AllocationSource = "Manual";
            }
            else
            {
                receipt.Department = dept;
                if (string.IsNullOrWhiteSpace(receipt.FileHandler))
                    receipt.FileHandler = handler;

                var email = _mailInfo.LookupEmail(receipt.FileHandler, _mailLookup);
                receipt.EmailAddress = email;
                receipt.NotificationRecipient = _mailInfo.ResolveNotificationRecipient(dept, email, _settings);
                receipt.Status = ProcessingStatus.Matched;
                receipt.MatchingNotes = $"Manually matched to {dept}";
                receipt.IsManualAllocation = true;
                receipt.AllocationSource = "Manual";
                receipt.AllocatedBy = Environment.UserName;
                receipt.AllocatedAt = DateTime.Now;
            }

            UpdateCounts();
            ApplyFilter();
            StatusMessage = $"Matched {reference} for receipt {receipt.AmountFormatted}.";
            await AutoSaveAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Match failed: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ClearManual(object? parameter)
    {
        if (parameter is not BankReceipt receipt) return;
        receipt.ManualReference = null;
        receipt.IsManualAllocation = false;
        receipt.FileHandler = null;
        receipt.Department = null;
        receipt.EmailAddress = null;
        receipt.NotificationRecipient = null;
        receipt.Status = ProcessingStatus.Unmatched;
        receipt.MatchingNotes = "Manual allocation cleared";
        receipt.AllocationSource = null;
        UpdateCounts();
        ApplyFilter();
        await AutoSaveAsync();
    }

    [RelayCommand]
    private void ExpandSplit(object? parameter)
    {
        if (parameter is not BankReceipt receipt) return;
        receipt.IsExpanded = !receipt.IsExpanded;
        receipt.IsSplit = true;
        if (receipt.Allocations.Count == 0)
            receipt.Allocations.Add(new AllocationLine { AllocatedAmount = 0 });
        _ = AutoSaveAsync();
    }

    [RelayCommand]
    private void AddAllocation(object? parameter)
    {
        if (parameter is not BankReceipt receipt) return;
        receipt.Allocations.Add(new AllocationLine());
        receipt.RefreshAllocationTotals();
    }

    [RelayCommand]
    private void RemoveAllocation(object? parameter)
    {
        if (parameter is not AllocationLine line) return;
        foreach (var receipt in Receipts)
        {
            if (receipt.Allocations.Contains(line))
            {
                receipt.Allocations.Remove(line);
                receipt.RefreshAllocationTotals();
                StatusMessage = "Allocation line removed.";
                _ = AutoSaveAsync();
                break;
            }
        }
    }

    [RelayCommand]
    private void RefreshAllocation(object? parameter)
    {
        if (parameter is not BankReceipt receipt) return;
        receipt.RefreshAllocationTotals();
        if (receipt.IsOverAllocated)
            StatusMessage = $"Overallocated by {Math.Abs(receipt.RemainingBalance):C2}. Cannot finalise.";
        else if (receipt.IsFullyAllocated)
            StatusMessage = "Fully allocated – balances to the penny.";
        else
            StatusMessage = $"Draft saved. Remaining: {receipt.RemainingFormatted}.";
        UpdateCounts();
        ApplyFilter();
    }

    [RelayCommand]
    private void MatchAllocation(object? parameter)
    {
        if (parameter is not AllocationLine line || string.IsNullOrWhiteSpace(line.CaseReference)) return;

        try
        {
            var caseLookup = _caseLookup.LoadAllCases(_settings.PiPath, _settings.HdrPath, _settings.ConvPath);
            var reference = line.CaseReference.Trim().ToUpperInvariant();
            var (dept, handler, notes) = _caseLookup.Lookup(reference, caseLookup);

            if (dept == null)
            {
                line.Status = ProcessingStatus.RequiresReview;
                line.MatchingNotes = notes ?? "Not found";
            }
            else
            {
                line.Department = dept;
                if (string.IsNullOrWhiteSpace(line.FileHandler))
                    line.FileHandler = handler;
                var email = _mailInfo.LookupEmail(line.FileHandler, _mailLookup);
                line.EmailAddress = email;
                line.NotificationRecipient = _mailInfo.ResolveNotificationRecipient(dept, email, _settings);
                line.Status = ProcessingStatus.Matched;
                line.MatchingNotes = $"Matched to {dept}";
                line.AllocationSource = "Manual";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Allocation match failed: {ex.Message}";
        }
    }
}
