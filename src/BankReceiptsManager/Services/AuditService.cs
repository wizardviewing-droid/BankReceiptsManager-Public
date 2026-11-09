using ClosedXML.Excel;

namespace BankReceiptsManager.Services;

public class AuditService
{
    private readonly string _auditPath;

    public AuditService(string storageFolder)
    {
        _auditPath = Path.Combine(storageFolder, "Audit Log.xlsx");
    }

    public void Log(string action, string receiptId, string? oldValue, string? newValue)
    {
        try
        {
            var backup = _auditPath + ".bak";
            if (File.Exists(_auditPath))
                File.Copy(_auditPath, backup, true);

            using var workbook = File.Exists(_auditPath) ? new XLWorkbook(_auditPath) : new XLWorkbook();
            var sheet = workbook.Worksheets.FirstOrDefault(w => w.Name == "Audit")
                        ?? workbook.Worksheets.Add("Audit");

            if (sheet.LastRowUsed() == null || sheet.Cell(1, 1).IsEmpty())
            {
                sheet.Cell(1, 1).Value = "Timestamp";
                sheet.Cell(1, 2).Value = "Username";
                sheet.Cell(1, 3).Value = "Action";
                sheet.Cell(1, 4).Value = "ReceiptId";
                sheet.Cell(1, 5).Value = "OldValue";
                sheet.Cell(1, 6).Value = "NewValue";
            }

            var row = (sheet.LastRowUsed()?.RowNumber() ?? 1) + 1;
            sheet.Cell(row, 1).Value = DateTime.Now;
            sheet.Cell(row, 2).Value = Environment.UserName;
            sheet.Cell(row, 3).Value = action;
            sheet.Cell(row, 4).Value = receiptId;
            sheet.Cell(row, 5).Value = oldValue ?? "";
            sheet.Cell(row, 6).Value = newValue ?? "";

            workbook.SaveAs(_auditPath);
        }
        catch
        {
            // Audit failure should not block the main save; it is logged silently
        }
    }
}
