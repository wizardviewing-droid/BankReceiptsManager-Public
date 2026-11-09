using System.Text.RegularExpressions;

namespace BankReceiptsManager.Services;

public class ReferenceExtractionService
{
    // Matches 6 digits optionally followed by a single letter
    // Examples: 688263, 638328A, FMAN/686800/MR B, MD/684966
    private static readonly Regex ReferenceRegex = new(
        @"\b(\d{6}[A-Za-z]?)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public List<string> ExtractCandidates(string details, string customerReference)
    {
        var text = $"{details} {customerReference}";
        var matches = ReferenceRegex.Matches(text);
        return matches.Select(m => m.Groups[1].Value.ToUpperInvariant())
                      .Distinct()
                      .ToList();
    }

    public string? SelectBestCandidate(List<string> candidates, IReadOnlyDictionary<string, CaseReportEntry> caseLookup)
    {
        if (candidates.Count == 0) return null;

        var valid = candidates.Where(c => caseLookup.ContainsKey(c)).ToList();

        if (valid.Count == 1)
            return valid[0];

        if (valid.Count > 1)
            return null; // multiple valid → require review

        // No exact match; return first candidate for manual review if only one plausible
        return candidates.Count == 1 ? candidates[0] : null;
    }
}
