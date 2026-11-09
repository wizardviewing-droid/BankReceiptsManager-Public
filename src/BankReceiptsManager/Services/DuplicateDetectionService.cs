namespace BankReceiptsManager.Services;

public class DuplicateDetectionService
{
    // Match key: date (date only) + amount + normalised description
    public static string CreateMatchKey(DateTime date, decimal amount, string description)
    {
        var normDesc = description.Trim().Replace("  ", " ").ToUpperInvariant();
        return $"{date:yyyy-MM-dd}|{amount:F2}|{normDesc}";
    }

    public void DetectDuplicates(List<BankReceipt> current, List<BankReceipt> history)
    {
        var historyGroups = history
            .GroupBy(r => CreateMatchKey(r.StatementDate, r.ReceiptAmount, r.TransactionDetails))
            .ToDictionary(g => g.Key, g => g.Count());

        var currentGroups = current
            .GroupBy(r => CreateMatchKey(r.StatementDate, r.ReceiptAmount, r.TransactionDetails))
            .ToList();

        foreach (var group in currentGroups)
        {
            var key = group.Key;
            var historicalCount = historyGroups.GetValueOrDefault(key, 0);
            var items = group.OrderBy(r => r.StatementDate).ThenBy(r => r.Id).ToList();

            for (int i = 0; i < items.Count; i++)
            {
                var receipt = items[i];
                var occurrence = historicalCount + i + 1;
                receipt.DuplicateOccurrence = occurrence;

                if (i < historicalCount)
                {
                    receipt.IsDuplicate = true;
                    receipt.Status = ProcessingStatus.Reviewed; // previously processed
                    receipt.MatchingNotes = $"Previously processed (occurrence {occurrence})";
                }
            }
        }
    }
}
