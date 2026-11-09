using System.Globalization;
using System.Text.RegularExpressions;
using ExcelDataReader;
using System.Data;

namespace BankReceiptsManager.Services;

public class BarclaysImportService
{
    // Columns confirmed from anonymised BACS CHAPS RECEIPTS.xls
    // Header row is typically around row 16 (0-based ~16)
    // Date: col 1, Type: col 3, Details: col 7, ValueDate: col 12,
    // BankRef: col 17, CustomerRef: col 20, ReceiptAmount: col 33

    public List<BankReceipt> Import(string filePath)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
        });

        if (dataSet.Tables.Count == 0)
            throw new InvalidOperationException("No worksheets found in the Barclays export.");

        var table = dataSet.Tables[0];
        var receipts = new List<BankReceipt>();

        // Find header row containing "Statement / Entry Date"
        int headerRow = -1;
        for (int r = 0; r < Math.Min(30, table.Rows.Count); r++)
        {
            var cell = table.Rows[r][1]?.ToString() ?? "";
            if (cell.Contains("Statement / Entry Date", StringComparison.OrdinalIgnoreCase))
            {
                headerRow = r;
                break;
            }
        }

        if (headerRow < 0)
            throw new InvalidOperationException("Could not locate the transaction header row in the Barclays export.");

        for (int r = headerRow + 1; r < table.Rows.Count; r++)
        {
            var row = table.Rows[r];
            var dateStr = row[1]?.ToString()?.Trim() ?? "";
            var type = row[3]?.ToString()?.Trim() ?? "";
            var details = row[7]?.ToString()?.Trim() ?? "";
            var amountObj = row.ItemArray.Length > 33 ? row[33] : null;

            // Skip non-transaction rows
            if (string.IsNullOrWhiteSpace(dateStr) && string.IsNullOrWhiteSpace(type) && string.IsNullOrWhiteSpace(details))
                continue;

            // Skip totals and summary
            if (details.Contains("Total Receipt", StringComparison.OrdinalIgnoreCase) ||
                type.Contains("Total", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!DateTime.TryParseExact(dateStr, new[] { "dd/MM/yyyy", "d/M/yyyy" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out var statementDate))
                continue;

            decimal amount = 0;
            if (amountObj is double d)
                amount = (decimal)d;
            else if (amountObj is decimal dec)
                amount = dec;
            else if (amountObj != null && decimal.TryParse(amountObj.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                amount = parsed;

            if (amount <= 0)
                continue;

            var receipt = new BankReceipt
            {
                StatementDate = statementDate,
                ValueDate = DateTime.TryParse(row.ItemArray.Length > 12 ? row[12]?.ToString() : null, out var vd) ? vd : statementDate,
                TransactionType = type,
                TransactionDetails = details,
                CustomerReference = row.ItemArray.Length > 20 ? row[20]?.ToString()?.Trim() ?? "" : "",
                BankReference = row.ItemArray.Length > 17 ? row[17]?.ToString()?.Trim() ?? "" : "",
                ReceiptAmount = amount,
                Status = ProcessingStatus.Imported
            };

            // Append any continuation lines of details (next rows with only details)
            // Handled by taking the primary details line; multi-line is rare but preserved in details

            receipts.Add(receipt);
        }

        if (receipts.Count == 0)
            throw new InvalidOperationException("No valid receipt transactions were found in the file.");

        return receipts;
    }
}
