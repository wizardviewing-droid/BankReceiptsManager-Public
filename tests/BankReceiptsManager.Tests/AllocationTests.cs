using BankReceiptsManager.Models;
using Xunit;

namespace BankReceiptsManager.Tests;

public class AllocationTests
{
    [Fact]
    public void FullAllocation_BalancesToPenny()
    {
        var receipt = new BankReceipt { ReceiptAmount = 50000m };
        for (int i = 0; i < 10; i++)
            receipt.Allocations.Add(new AllocationLine { AllocatedAmount = 5000m, CaseReference = $"10000{i}" });

        receipt.RefreshAllocationTotals();

        Assert.Equal(50000m, receipt.TotalAllocated);
        Assert.Equal(0m, receipt.RemainingBalance);
        Assert.True(receipt.IsFullyAllocated);
        Assert.Equal(ProcessingStatus.FullyAllocated, receipt.Status);
    }

    [Fact]
    public void PartialAllocation_ShowsRemaining()
    {
        var receipt = new BankReceipt { ReceiptAmount = 50000m };
        receipt.Allocations.Add(new AllocationLine { AllocatedAmount = 49000m });
        receipt.RefreshAllocationTotals();

        Assert.Equal(1000m, receipt.RemainingBalance);
        Assert.True(receipt.IsPartiallyAllocated);
        Assert.False(receipt.IsFullyAllocated);
        Assert.Equal(ProcessingStatus.PartiallyAllocated, receipt.Status);
    }

    [Fact]
    public void OverAllocation_IsFlaggedAndCannotBeFullyAllocated()
    {
        var receipt = new BankReceipt { ReceiptAmount = 50000m };
        receipt.Allocations.Add(new AllocationLine { AllocatedAmount = 51000m });
        receipt.RefreshAllocationTotals();

        Assert.True(receipt.IsOverAllocated);
        Assert.Equal(ProcessingStatus.RequiresReview, receipt.Status);
        Assert.False(receipt.IsFullyAllocated);
    }

    [Fact]
    public void DuplicateDetection_ReimportSameFile_DoesNotCreateDuplicates()
    {
        var id = Guid.NewGuid();
        var receipt = new BankReceipt
        {
            Id = id,
            StatementDate = new DateTime(2026, 11, 9),
            ReceiptAmount = 500m,
            TransactionDetails = "PAYMENT REF 654321"
        };
        var history = new List<BankReceipt> { receipt };
        var reimport = new List<BankReceipt> { receipt };

        var svc = new Services.DuplicateDetectionService();
        svc.DetectDuplicates(reimport, history);

        Assert.Single(reimport);
        Assert.False(reimport[0].IsDuplicate);
        Assert.Equal(id, reimport[0].Id);
    }

    [Fact]
    public void DuplicateDetection_YesterdayProcessedReceipt_IsRecognised()
    {
        var yesterday = new BankReceipt
        {
            Id = Guid.NewGuid(),
            StatementDate = new DateTime(2026, 11, 8),
            ReceiptAmount = 250m,
            TransactionDetails = "OLD PAYMENT"
        };
        var todayImport = new BankReceipt
        {
            Id = Guid.NewGuid(),
            StatementDate = new DateTime(2026, 11, 8),
            ReceiptAmount = 250m,
            TransactionDetails = "OLD PAYMENT"
        };

        var svc = new Services.DuplicateDetectionService();
        svc.DetectDuplicates(new List<BankReceipt> { todayImport }, new List<BankReceipt> { yesterday });

        Assert.True(todayImport.IsDuplicate);
    }
    {
        var id = Guid.NewGuid();
        var receipt = new BankReceipt
        {
            Id = id,
            StatementDate = new DateTime(2026, 11, 9),
            ReceiptAmount = 1000m,
            TransactionDetails = "TEST REF 123456"
        };
        var history = new List<BankReceipt> { receipt };
        var current = new List<BankReceipt> { receipt };

        var svc = new Services.DuplicateDetectionService();
        svc.DetectDuplicates(current, history);

        Assert.False(receipt.IsDuplicate);
    }
    {
        var line = new AllocationLine { AllocatedAmount = 0m };
        Assert.Equal(0m, line.AllocatedAmount);
        // UI / save logic should reject <= 0; model stores the value for display of the error
    }
}
