using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BankReceiptsManager.Models;

public enum ProcessingStatus
{
    Imported,
    Reviewed,
    Matched,
    RequiresReview,
    Unmatched,
    PartiallyAllocated,
    FullyAllocated,
    DraftCreated,
    Sent,
    Unresolved,
    Returned
}

public partial class AllocationLine : ObservableObject
{
    [ObservableProperty] private Guid id = Guid.NewGuid();
    [ObservableProperty] private string? caseReference;
    [ObservableProperty] private decimal allocatedAmount;
    [ObservableProperty] private string? department;
    [ObservableProperty] private string? fileHandler;
    [ObservableProperty] private string? emailAddress;
    [ObservableProperty] private string? notificationRecipient;
    [ObservableProperty] private ProcessingStatus status = ProcessingStatus.Unmatched;
    [ObservableProperty] private string? matchingNotes;
    [ObservableProperty] private bool isManual;
    [ObservableProperty] private string? allocationSource;

    public string AmountFormatted => AllocatedAmount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
}

public partial class BankReceipt : ObservableObject
{
    [ObservableProperty] private Guid id = Guid.NewGuid();
    [ObservableProperty] private DateTime statementDate;
    [ObservableProperty] private DateTime valueDate;
    [ObservableProperty] private string transactionType = string.Empty;
    [ObservableProperty] private string transactionDetails = string.Empty;
    [ObservableProperty] private string customerReference = string.Empty;
    [ObservableProperty] private string bankReference = string.Empty;
    [ObservableProperty] private decimal receiptAmount;
    [ObservableProperty] private string? extractedReference;
    [ObservableProperty] private string? manualReference;
    [ObservableProperty] private string? department;
    [ObservableProperty] private string? fileHandler;
    [ObservableProperty] private string? emailAddress;
    [ObservableProperty] private string? notificationRecipient;
    [ObservableProperty] private ProcessingStatus status = ProcessingStatus.Imported;
    [ObservableProperty] private string? matchingNotes;
    [ObservableProperty] private bool isDuplicate;
    [ObservableProperty] private int duplicateOccurrence;
    [ObservableProperty] private string? originalExtractedReference;
    [ObservableProperty] private bool isManualAllocation;
    [ObservableProperty] private string? allocationSource;
    [ObservableProperty] private string? manualAllocationReason;
    [ObservableProperty] private string? allocatedBy;
    [ObservableProperty] private DateTime? allocatedAt;
    [ObservableProperty] private bool isRemovedFromBatch;
    [ObservableProperty] private bool isExpanded;
    [ObservableProperty] private bool isSplit;

    public ObservableCollection<AllocationLine> Allocations { get; } = new();

    public decimal TotalAllocated => Allocations.Sum(a => a.AllocatedAmount);
    public decimal RemainingBalance => ReceiptAmount - TotalAllocated;
    public bool IsFullyAllocated => Allocations.Count > 0 && RemainingBalance == 0m;
    public bool IsPartiallyAllocated => Allocations.Count > 0 && RemainingBalance > 0m && RemainingBalance < ReceiptAmount;
    public bool IsOverAllocated => RemainingBalance < 0m;

    public string DisplayStatus => Status.ToString();
    public string AmountFormatted => ReceiptAmount.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
    public string AllocatedFormatted => TotalAllocated.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
    public string RemainingFormatted => RemainingBalance.ToString("C2", System.Globalization.CultureInfo.GetCultureInfo("en-GB"));
    public string DateFormatted => StatementDate.ToString("dd/MM/yyyy");

    public void RefreshAllocationTotals()
    {
        OnPropertyChanged(nameof(TotalAllocated));
        OnPropertyChanged(nameof(RemainingBalance));
        OnPropertyChanged(nameof(IsFullyAllocated));
        OnPropertyChanged(nameof(IsPartiallyAllocated));
        OnPropertyChanged(nameof(IsOverAllocated));
        OnPropertyChanged(nameof(AllocatedFormatted));
        OnPropertyChanged(nameof(RemainingFormatted));

        if (IsOverAllocated)
            Status = ProcessingStatus.RequiresReview;
        else if (IsFullyAllocated)
            Status = ProcessingStatus.FullyAllocated;
        else if (IsPartiallyAllocated)
            Status = ProcessingStatus.PartiallyAllocated;
    }
}
