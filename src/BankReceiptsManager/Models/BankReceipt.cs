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
    DraftCreated,
    Sent,
    Unresolved,
    Returned
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
    [ObservableProperty] private string? department;
    [ObservableProperty] private string? fileHandler;
    [ObservableProperty] private string? emailAddress;
    [ObservableProperty] private ProcessingStatus status = ProcessingStatus.Imported;
    [ObservableProperty] private string? matchingNotes;
    [ObservableProperty] private bool isDuplicate;
    [ObservableProperty] private int duplicateOccurrence;
    [ObservableProperty] private string? originalExtractedReference;
    [ObservableProperty] private string? manualAllocationReason;
    [ObservableProperty] private string? allocatedBy;
    [ObservableProperty] private DateTime? allocatedAt;

    public string DisplayStatus => Status.ToString();
    public string AmountFormatted => ReceiptAmount.ToString("C2");
}
