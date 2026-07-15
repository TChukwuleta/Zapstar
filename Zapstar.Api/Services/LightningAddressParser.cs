using System.Text.RegularExpressions;

namespace Zapstar.Api.Services;

public static partial class LightningAddressParser
{
    [GeneratedRegex(@"[a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}")]
    private static partial Regex AddressPattern();

    public static string? FindCandidate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        // Prefer anything immediately preceded by a lightning-bolt emoji or explicit prefix.
        var prioritized = Regex.Match(text, @"(?:⚡\s*|lightning:\s*|ln:\s*)([a-zA-Z0-9._-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})",
            RegexOptions.IgnoreCase);
        if (prioritized.Success)
            return prioritized.Groups[1].Value;

        var match = AddressPattern().Match(text);
        return match.Success ? match.Value : null;
    }
}
