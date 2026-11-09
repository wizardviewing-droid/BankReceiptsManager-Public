using System.Text.Json;

namespace BankReceiptsManager.Services;

/// <summary>
/// Simple JSON-based history for Milestone 1 (Excel storage comes in Milestone 2).
/// In production this will write to Y:\accounts\Receipts Manager\Receipts History.xlsx
/// </summary>
public class ReceiptHistoryService
{
    private readonly string _historyPath;

    public ReceiptHistoryService(string storageFolder)
    {
        Directory.CreateDirectory(storageFolder);
        _historyPath = Path.Combine(storageFolder, "receipts_history.json");
    }

    public List<BankReceipt> LoadHistory()
    {
        if (!File.Exists(_historyPath))
            return new List<BankReceipt>();

        try
        {
            var json = File.ReadAllText(_historyPath);
            return JsonSerializer.Deserialize<List<BankReceipt>>(json) ?? new();
        }
        catch
        {
            return new List<BankReceipt>();
        }
    }

    public void SaveHistory(List<BankReceipt> receipts)
    {
        var json = JsonSerializer.Serialize(receipts, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_historyPath, json);
    }
}
