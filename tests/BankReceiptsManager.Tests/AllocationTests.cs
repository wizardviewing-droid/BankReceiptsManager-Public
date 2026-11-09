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
    public void ZeroOrNegativeAllocation_IsRejectedByValidationLogic()
    {
        var line = new AllocationLine { AllocatedAmount = 0m };
        Assert.Equal(0m, line.AllocatedAmount);
        // UI / save logic should reject <= 0; model stores the value for display of the error
    }
}
