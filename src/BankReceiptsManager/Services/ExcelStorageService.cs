using ClosedXML.Excel;
using BankReceiptsManager.Models;

namespace BankReceiptsManager.Services;

public class ExcelStorageService
{
    private readonly string _storageFolder;
    private readonly string _lockFile;

    public ExcelStorageService(string storageFolder)
    {
        _storageFolder = storageFolder ?? throw new ArgumentNullException(nameof(storageFolder));
        Directory.CreateDirectory(_storageFolder);
        _lockFile = Path.Combine(_storageFolder, "processing.lock");
    }

    public bool TestConnection()
    {
        try
        {
            var testFile = Path.Combine(_storageFolder, ".write_test");
            File.WriteAllText(testFile, DateTime.Now.ToString("O"));
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private FileStream? _lockStream;

    public bool TryAcquireSessionLock(string userName)
    {
        try
        {
            _lockStream = new FileStream(_lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            var info = $"{userName}|{Environment.MachineName}|{DateTime.Now:O}";
            var bytes = System.Text.Encoding.UTF8.GetBytes(info);
            _lockStream.SetLength(0);
            _lockStream.Write(bytes, 0, bytes.Length);
            _lockStream.Flush();
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    public void ReleaseSessionLock()
    {
        _lockStream?.Dispose();
        _lockStream = null;
        if (File.Exists(_lockFile))
            File.Delete(_lockFile);
    }

    public string? GetLockOwner()
    {
        if (!File.Exists(_lockFile)) return null;
        var text = File.ReadAllText(_lockFile);
        var parts = text.Split('|');
        return parts.Length > 0 ? parts[0] : null;
    }

    public void ReleaseLock()
    {
        if (File.Exists(_lockFile))
            File.Delete(_lockFile);
    }

    public string GetDailyFilePath(DateTime processingDate)
        => Path.Combine(_storageFolder, $"Receipts {processingDate:dd-MM-yyyy}.xlsx");

    public string MasterHistoryPath => Path.Combine(_storageFolder, "Receipts History.xlsx");

    public void SaveDailyWorkbook(DateTime processingDate, IEnumerable<BankReceipt> receipts)
    {
        var path = GetDailyFilePath(processingDate);
        var backup = path + ".bak";

        if (File.Exists(path))
            File.Copy(path, backup, true);

        using var workbook = File.Exists(path) ? new XLWorkbook(path) : new XLWorkbook();

        var receiptsSheet = workbook.Worksheets.FirstOrDefault(w => w.Name == "Receipts")
                            ?? workbook.Worksheets.Add("Receipts");
        var allocationsSheet = workbook.Worksheets.FirstOrDefault(w => w.Name == "Allocations")
                               ?? workbook.Worksheets.Add("Allocations");

        // Clear and rewrite (safe because we have the lock)
        receiptsSheet.Clear();
        allocationsSheet.Clear();

        WriteReceiptsHeader(receiptsSheet);
        WriteAllocationsHeader(allocationsSheet);

        int rRow = 2, aRow = 2;
        foreach (var receipt in receipts.Where(r => !r.IsRemovedFromBatch))
        {
            WriteReceiptRow(receiptsSheet, rRow++, receipt);
            foreach (var alloc in receipt.Allocations)
                WriteAllocationRow(allocationsSheet, aRow++, receipt.Id, alloc);
        }

        workbook.SaveAs(path);
    }

    public List<BankReceipt> LoadDailyWorkbook(DateTime processingDate)
    {
        var path = GetDailyFilePath(processingDate);
        if (!File.Exists(path))
            return new List<BankReceipt>();

        using var workbook = new XLWorkbook(path);
        var receipts = new List<BankReceipt>();
        var sheet = workbook.Worksheet("Receipts");
        if (sheet == null) return receipts;

        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var receipt = ReadReceiptRow(row);
            if (receipt != null)
                receipts.Add(receipt);
        }

        var allocSheet = workbook.Worksheet("Allocations");
        if (allocSheet != null)
        {
            foreach (var row in allocSheet.RowsUsed().Skip(1))
            {
                var (parentId, line) = ReadAllocationRow(row);
                var parent = receipts.FirstOrDefault(r => r.Id == parentId);
                parent?.Allocations.Add(line);
                parent?.RefreshAllocationTotals();
            }
        }

        return receipts;
    }

    public void AppendToMasterHistory(IEnumerable<BankReceipt> receipts)
    {
        var path = MasterHistoryPath;
        var backup = path + ".bak";
        if (File.Exists(path))
            File.Copy(path, backup, true);

        using var workbook = File.Exists(path) ? new XLWorkbook(path) : new XLWorkbook();
        var sheet = workbook.Worksheets.FirstOrDefault(w => w.Name == "History")
                    ?? workbook.Worksheets.Add("History");

        if (sheet.LastRowUsed() == null || sheet.Cell(1, 1).IsEmpty())
            WriteHistoryHeader(sheet);

        var existingKeys = new HashSet<string>();
        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            var key = row.Cell(1).GetString(); // match key stored in col A
            if (!string.IsNullOrEmpty(key))
                existingKeys.Add(key);
        }

        int nextRow = (sheet.LastRowUsed()?.RowNumber() ?? 1) + 1;
        foreach (var receipt in receipts.Where(r => !r.IsRemovedFromBatch))
        {
            var key = DuplicateDetectionService.CreateMatchKey(receipt.StatementDate, receipt.ReceiptAmount, receipt.TransactionDetails);
            if (existingKeys.Contains(key)) continue;

            sheet.Cell(nextRow, 1).Value = key;
            sheet.Cell(nextRow, 2).Value = receipt.Id.ToString();
            sheet.Cell(nextRow, 3).Value = receipt.StatementDate;
            sheet.Cell(nextRow, 4).Value = receipt.ReceiptAmount;
            sheet.Cell(nextRow, 5).Value = receipt.TransactionDetails;
            sheet.Cell(nextRow, 6).Value = receipt.Status.ToString();
            sheet.Cell(nextRow, 7).Value = DateTime.Today;
            nextRow++;
            existingKeys.Add(key);
        }

        workbook.SaveAs(path);
    }

    public List<BankReceipt> LoadMasterHistory()
    {
        var path = MasterHistoryPath;
        if (!File.Exists(path))
            return new List<BankReceipt>();

        using var workbook = new XLWorkbook(path);
        var sheet = workbook.Worksheet("History");
        if (sheet == null) return new List<BankReceipt>();

        var list = new List<BankReceipt>();
        foreach (var row in sheet.RowsUsed().Skip(1))
        {
            list.Add(new BankReceipt
            {
                Id = Guid.TryParse(row.Cell(2).GetString(), out var id) ? id : Guid.NewGuid(),
                StatementDate = row.Cell(3).GetDateTime(),
                ReceiptAmount = row.Cell(4).GetValue<decimal>(),
                TransactionDetails = row.Cell(5).GetString(),
                Status = Enum.TryParse<ProcessingStatus>(row.Cell(6).GetString(), out var s) ? s : ProcessingStatus.Reviewed
            });
        }
        return list;
    }

    // --- helpers ---
    private static void WriteReceiptsHeader(IXLWorksheet sheet)
    {
        var headers = new[] { "Id", "Date", "Amount", "Details", "CustomerRef", "ExtractedRef", "ManualRef", "Department", "FileHandler", "Email", "NotifyTo", "Status", "Source", "IsManual" };
        for (int i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];
    }

    private static void WriteReceiptRow(IXLWorksheet sheet, int row, BankReceipt r)
    {
        sheet.Cell(row, 1).Value = r.Id.ToString();
        sheet.Cell(row, 2).Value = r.StatementDate;
        sheet.Cell(row, 3).Value = r.ReceiptAmount;
        sheet.Cell(row, 4).Value = r.TransactionDetails;
        sheet.Cell(row, 5).Value = r.CustomerReference;
        sheet.Cell(row, 6).Value = r.ExtractedReference;
        sheet.Cell(row, 7).Value = r.ManualReference;
        sheet.Cell(row, 8).Value = r.Department;
        sheet.Cell(row, 9).Value = r.FileHandler;
        sheet.Cell(row, 10).Value = r.EmailAddress;
        sheet.Cell(row, 11).Value = r.NotificationRecipient;
        sheet.Cell(row, 12).Value = r.Status.ToString();
        sheet.Cell(row, 13).Value = r.AllocationSource;
        sheet.Cell(row, 14).Value = r.IsManualAllocation;
    }

    private static BankReceipt? ReadReceiptRow(IXLRow row)
    {
        if (row.Cell(1).IsEmpty()) return null;
        return new BankReceipt
        {
            Id = Guid.TryParse(row.Cell(1).GetString(), out var id) ? id : Guid.NewGuid(),
            StatementDate = row.Cell(2).GetDateTime(),
            ReceiptAmount = row.Cell(3).GetValue<decimal>(),
            TransactionDetails = row.Cell(4).GetString(),
            CustomerReference = row.Cell(5).GetString(),
            ExtractedReference = row.Cell(6).GetString(),
            ManualReference = row.Cell(7).GetString(),
            Department = row.Cell(8).GetString(),
            FileHandler = row.Cell(9).GetString(),
            EmailAddress = row.Cell(10).GetString(),
            NotificationRecipient = row.Cell(11).GetString(),
            Status = Enum.TryParse<ProcessingStatus>(row.Cell(12).GetString(), out var s) ? s : ProcessingStatus.Imported,
            AllocationSource = row.Cell(13).GetString(),
            IsManualAllocation = row.Cell(14).GetBoolean()
        };
    }

    private static void WriteAllocationsHeader(IXLWorksheet sheet)
    {
        var headers = new[] { "ParentId", "CaseReference", "AllocatedAmount", "Department", "FileHandler", "Email", "NotifyTo", "Status" };
        for (int i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];
    }

    private static void WriteAllocationRow(IXLWorksheet sheet, int row, Guid parentId, AllocationLine a)
    {
        sheet.Cell(row, 1).Value = parentId.ToString();
        sheet.Cell(row, 2).Value = a.CaseReference;
        sheet.Cell(row, 3).Value = a.AllocatedAmount;
        sheet.Cell(row, 4).Value = a.Department;
        sheet.Cell(row, 5).Value = a.FileHandler;
        sheet.Cell(row, 6).Value = a.EmailAddress;
        sheet.Cell(row, 7).Value = a.NotificationRecipient;
        sheet.Cell(row, 8).Value = a.Status.ToString();
    }

    private static (Guid parentId, AllocationLine line) ReadAllocationRow(IXLRow row)
    {
        var parentId = Guid.TryParse(row.Cell(1).GetString(), out var id) ? id : Guid.Empty;
        var line = new AllocationLine
        {
            CaseReference = row.Cell(2).GetString(),
            AllocatedAmount = row.Cell(3).GetValue<decimal>(),
            Department = row.Cell(4).GetString(),
            FileHandler = row.Cell(5).GetString(),
            EmailAddress = row.Cell(6).GetString(),
            NotificationRecipient = row.Cell(7).GetString(),
            Status = Enum.TryParse<ProcessingStatus>(row.Cell(8).GetString(), out var s) ? s : ProcessingStatus.Unmatched
        };
        return (parentId, line);
    }

    private static void WriteHistoryHeader(IXLWorksheet sheet)
    {
        var headers = new[] { "MatchKey", "Id", "StatementDate", "Amount", "Details", "Status", "ProcessingDate" };
        for (int i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];
    }
}
