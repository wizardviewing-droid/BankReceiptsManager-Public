using BankReceiptsManager.Models;

namespace BankReceiptsManager.Services;

public class DuplicateDetectionService
{
    public static string CreateMatchKey(DateTime date, decimal amount, string details)
    {
        var normalised = (details ?? string.Empty).Trim().ToUpperInvariant();
        normalised = string.Join(" ", normalised.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
        return $"{date:yyyy-MM-dd}|{amount:F2}|{normalised}";
    }

    public void DetectDuplicates(List<BankReceipt> current, List<BankReceipt> history)
    {
        var historyById = history.Where(h => h.Id != Guid.Empty).ToDictionary(h => h.Id, h => h);
        var historyGroups = history
            .GroupBy(r => CreateMatchKey(r.StatementDate, r.ReceiptAmount, r.TransactionDetails))
            .ToDictionary(g => g.Key, g => g.Select(r => r.Id).ToHashSet());

        var currentGroups = current
            .GroupBy(r => CreateMatchKey(r.StatementDate, r.ReceiptAmount, r.TransactionDetails));

        foreach (var group in currentGroups)
        {
            var key = group.Key;
            var historicalIds = historyGroups.GetValueOrDefault(key, new HashSet<Guid>());
            var items = group.OrderBy(r => r.StatementDate).ThenBy(r => r.Id).ToList();

            for (int i = 0; i < items.Count; i++)
            {
                var receipt = items[i];

                // Never mark a receipt as a duplicate of itself
                if (historyById.ContainsKey(receipt.Id))
                {
                    receipt.IsDuplicate = false;
                    continue;
                }

                // Count historical occurrences that are not this receipt
                var otherHistorical = historicalIds.Count(id => id != receipt.Id);
                receipt.DuplicateOccurrence = otherHistorical + i + 1;

                if (otherHistorical > 0 && i < otherHistorical)
                {
                    receipt.IsDuplicate = true;
                    receipt.Status = ProcessingStatus.Reviewed;
                    receipt.MatchingNotes = $"Previously processed (occurrence {receipt.DuplicateOccurrence})";
                }
                else
                {
                    receipt.IsDuplicate = false;
                }
            }
        }
    }
}
