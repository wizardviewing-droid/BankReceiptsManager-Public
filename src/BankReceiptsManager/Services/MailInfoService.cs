using ExcelDataReader;
using System.Data;

namespace BankReceiptsManager.Services;

public class MailInfoService
{
    public Dictionary<string, string> Load(string mailInfoPath)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(mailInfoPath))
            throw new FileNotFoundException($"Mailinfo file not found: {mailInfoPath}");

        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        using var stream = File.Open(mailInfoPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        var table = reader.AsDataSet().Tables[0];

        for (int r = 1; r < table.Rows.Count; r++)
        {
            var name = table.Rows[r][0]?.ToString()?.Trim() ?? "";
            var email = table.Rows[r].ItemArray.Length > 1 ? table.Rows[r][1]?.ToString()?.Trim() ?? "" : "";

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                continue;

            var key = name.Trim();
            if (!map.ContainsKey(key))
                map[key] = email;
        }

        return map;
    }

    public List<string> GetHandlerNames(Dictionary<string, string> mailInfo)
        => mailInfo.Keys.OrderBy(k => k).ToList();

    public string? LookupEmail(string? fileHandler, Dictionary<string, string> mailInfo)
    {
        if (string.IsNullOrWhiteSpace(fileHandler))
            return null;

        var key = fileHandler.Trim();
        if (mailInfo.TryGetValue(key, out var email))
            return email;

        var loose = mailInfo.FirstOrDefault(kvp =>
            kvp.Key.Contains(key, StringComparison.OrdinalIgnoreCase) ||
            key.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase));

        return loose.Value;
    }

    public string ResolveNotificationRecipient(string? department, string? individualEmail, AppSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(department) &&
            settings.DepartmentEmailOverrides.TryGetValue(department, out var overrideEmail) &&
            !string.IsNullOrWhiteSpace(overrideEmail))
        {
            return overrideEmail;
        }
        return individualEmail ?? "";
    }
}
