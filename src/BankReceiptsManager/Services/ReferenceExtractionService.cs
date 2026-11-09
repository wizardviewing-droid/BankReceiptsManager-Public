using System.Text.RegularExpressions;

namespace BankReceiptsManager.Services;

public class ReferenceExtractionService
{
    // Finds 6-digit sequences (optionally followed by a single letter) even when embedded
    // e.g. LHA675427 → 675427, FMAN/686800/MR B → 686800, MD/684966 → 684966, 638328A
    private static readonly Regex ReferenceRegex = new(
        @"(?<!\d)(\d{6})([A-Za-z])?(?!\d)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public List<string> ExtractCandidates(string details, string customerReference)
    {
        var text = $"{details} {customerReference}";
        var matches = ReferenceRegex.Matches(text);
        var candidates = new List<string>();

        foreach (Match m in matches)
        {
            var digits = m.Groups[1].Value;
            var letter = m.Groups[2].Success ? m.Groups[2].Value.ToUpperInvariant() : "";
            candidates.Add(digits + letter);
        }

        return candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public string? SelectBestCandidate(List<string> candidates, IReadOnlyDictionary<string, List<CaseReportEntry>> caseLookup)
    {
        if (candidates.Count == 0) return null;

        var valid = candidates
            .Where(c => caseLookup.ContainsKey(c.ToUpperInvariant()))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (valid.Count == 1)
            return valid[0].ToUpperInvariant();

        return null; // zero or multiple valid → caller decides
    }
}
