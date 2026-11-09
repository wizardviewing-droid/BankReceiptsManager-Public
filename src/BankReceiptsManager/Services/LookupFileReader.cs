using ExcelDataReader;
using System.Data;

namespace BankReceiptsManager.Services;

/// <summary>
/// Opens the live departmental lookup files (PI / HDR / Conveyancing case reports
/// and the Mailinfo workbook). CSV files must be read with the CSV reader;
/// .xls / .xlsx files use the binary/OpenXML reader.
/// </summary>
internal static class LookupFileReader
{
    public static DataTable ReadFirstSheet(string path)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var ext = Path.GetExtension(path);
        using IExcelDataReader reader = string.Equals(ext, ".csv", StringComparison.OrdinalIgnoreCase)
            ? ExcelReaderFactory.CreateCsvReader(stream)
            : ExcelReaderFactory.CreateReader(stream);

        var dataSet = reader.AsDataSet();
        if (dataSet.Tables.Count == 0)
            throw new InvalidOperationException($"No data found in lookup file: {path}");

        return dataSet.Tables[0];
    }
}
