using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace BankReceiptsManager.ViewModels;

public partial class ReviewViewModel : ObservableObject
{
    private readonly MainViewModel _main;
    private readonly Services.SettingsService _settingsService = new();
    private readonly Services.CaseLookupService _caseLookup = new();
    private readonly Services.MailInfoService _mailInfo = new();
    private readonly Services.ReferenceExtractionService _extractor = new();
    private readonly Services.DuplicateDetectionService _duplicates = new();

    public ObservableCollection<BankReceipt> Receipts { get; } = new();
    public ObservableCollection<BankReceipt> FilteredReceipts { get; } = new();

    [ObservableProperty] private string filter = "All";
    [ObservableProperty] private string statusSummary = "No receipts loaded";
    [ObservableProperty] private string matchingStatus = "Not run";

    public ReviewViewModel(MainViewModel main)
    {
        _main = main;
    }

    public void LoadReceipts(List<BankReceipt> receipts)
    {
        Receipts.Clear();
        foreach (var r in receipts)
            Receipts.Add(r);

        ApplyFilter();
        UpdateSummary();
    }

    partial void OnFilterChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        FilteredReceipts.Clear();
        var query = Receipts.AsEnumerable();

        query = Filter switch
        {
            "Matched" => query.Where(r => r.Status == ProcessingStatus.Matched),
            "Requires Review" => query.Where(r => r.Status == ProcessingStatus.RequiresReview),
            "Unmatched" => query.Where(r => r.Status == ProcessingStatus.Unmatched),
            "Duplicates" => query.Where(r => r.IsDuplicate),
            _ => query
        };

        foreach (var r in query)
            FilteredReceipts.Add(r);
    }

    private void UpdateSummary()
    {
        var matched = Receipts.Count(r => r.Status == ProcessingStatus.Matched);
        var review = Receipts.Count(r => r.Status == ProcessingStatus.RequiresReview);
        var unmatched = Receipts.Count(r => r.Status == ProcessingStatus.Unmatched);
        statusSummary = $"Total: {Receipts.Count} | Matched: {matched} | Review: {review} | Unmatched: {unmatched}";
    }

    [RelayCommand]
    private void RunMatching()
    {
        try
        {
            var settings = _settingsService.Load();
            matchingStatus = "Loading reports...";

            var caseLookup = _caseLookup.LoadAllCases(settings.PiPath, settings.HdrPath, settings.ConvPath);
            var mailLookup = _mailInfo.Load(settings.MailInfoPath);

            // History for duplicates – currently local JSON; will move to Y: Excel in Milestone 2
            var historyService = new Services.ReceiptHistoryService(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BankReceiptsManager"));
            var history = historyService.LoadHistory();

            _duplicates.DetectDuplicates(Receipts.ToList(), history);

            foreach (var receipt in Receipts)
            {
                if (receipt.IsDuplicate)
                {
                    receipt.Status = ProcessingStatus.Reviewed;
                    continue;
                }

                var candidates = _extractor.ExtractCandidates(receipt.TransactionDetails, receipt.CustomerReference);

                if (candidates.Count == 0)
                {
                    receipt.ExtractedReference = null;
                    receipt.Status = ProcessingStatus.Unmatched;
                    receipt.MatchingNotes = "No 6-digit reference found";
                    continue;
                }

                if (candidates.Count > 1)
                {
                    receipt.ExtractedReference = string.Join(", ", candidates);
                    receipt.Status = ProcessingStatus.RequiresReview;
                    receipt.MatchingNotes = "Multiple candidate references – manual review required";
                    continue;
                }

                var reference = candidates[0];
                receipt.ExtractedReference = reference;
                receipt.OriginalExtractedReference = reference;

                var (dept, handler, notes) = _caseLookup.Lookup(reference, caseLookup);

                if (dept == null)
                {
                    receipt.Status = ProcessingStatus.RequiresReview;
                    receipt.MatchingNotes = notes ?? "Reference not found in departmental reports";
                    continue;
                }

                receipt.Department = dept;
                receipt.FileHandler = handler;

                var email = _mailInfo.LookupEmail(handler, mailLookup);
                if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                {
                    receipt.Status = ProcessingStatus.RequiresReview;
                    receipt.MatchingNotes = $"Matched to {dept} but no valid email for handler '{handler}'";
                    continue;
                }

                receipt.EmailAddress = email;
                receipt.Status = ProcessingStatus.Matched;
                receipt.MatchingNotes = $"Matched to {dept}";
            }

            ApplyFilter();
            UpdateSummary();
            matchingStatus = "Matching complete";
            _main.SetStatus("Matching completed against live reports.");
        }
        catch (Exception ex)
        {
            matchingStatus = "Error";
            _main.SetStatus($"Matching failed: {ex.Message}");
        }
    }
}
