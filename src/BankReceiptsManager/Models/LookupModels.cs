namespace BankReceiptsManager.Models;

public class CaseReportEntry
{
    public string CaseReference { get; set; } = string.Empty;
    public string FileHandler { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string SourceFile { get; set; } = string.Empty;
}

public class MailInfoEntry
{
    public string FileHandlerName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
}
