using System.Text.RegularExpressions;
using ExcelDataReader;
using System.Data;

namespace BankReceiptsManager.Services;

public class CaseLookupService
{
    public Dictionary<string, List<CaseReportEntry>> LoadAllCases(
        string piPath, string hdrPath, string convPath)
    {
        var lookup = new Dictionary<string, List<CaseReportEntry>>(StringComparer.OrdinalIgnoreCase);

        LoadReport(piPath, "PI", 0, 3, lookup);           // Col A=0, D=3
        LoadReport(hdrPath, "HDR", 0, 1, lookup);         // Col A=0, B=1
        LoadReport(convPath, "Conveyancing", 0, 2, lookup); // Col A=0, C=2

        return lookup;
    }

    private void LoadReport(string path, string department, int refCol, int handlerCol, Dictionary<string, List<CaseReportEntry>> lookup)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Case report not found: {path}");

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var table = reader.AsDataSet().Tables[0];

        for (int r = 1; r < table.Rows.Count; r++) // skip header
        {
            var caseRef = table.Rows[r][refCol]?.ToString()?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(caseRef) || !Regex.IsMatch(caseRef, @"^\d{6}[A-Za-z]?$"))
                continue;

            var handler = table.Rows[r].ItemArray.Length > handlerCol
                ? table.Rows[r][handlerCol]?.ToString()?.Trim() ?? ""
                : "";

            var key = caseRef.ToUpperInvariant();
            if (!lookup.TryGetValue(key, out var list))
            {
                list = new List<CaseReportEntry>();
                lookup[key] = list;
            }

            list.Add(new CaseReportEntry
            {
                CaseReference = key,
                FileHandler = handler,
                Department = department,
                SourceFile = Path.GetFileName(path)
            });
        }
    }

    public (string? Department, string? FileHandler, string? Notes) Lookup(string reference, Dictionary<string, List<CaseReportEntry>> lookup)
    {
        if (string.IsNullOrWhiteSpace(reference))
            return (null, null, "No reference extracted");

        var key = reference.ToUpperInvariant();
        if (!lookup.TryGetValue(key, out var matches) || matches.Count == 0)
            return (null, null, "Reference not found in any departmental report");

        var distinctDepts = matches.Select(m => m.Department).Distinct().ToList();
        if (distinctDepts.Count > 1)
            return (null, null, $"Reference found in multiple departments: {string.Join(", ", distinctDepts)}");

        var match = matches[0];
        return (match.Department, match.FileHandler, null);
    }
}
