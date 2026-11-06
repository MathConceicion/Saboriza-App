namespace Ganss.XSS;

using System.Text.RegularExpressions;

public sealed class HtmlSanitizer
{
    public HashSet<string> AllowedTags { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> AllowedAttributes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> AllowedSchemes { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var withoutScript = Regex.Replace(html, @"<script[^>]*>.*?</script>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        withoutScript = Regex.Replace(withoutScript, @"<[^>]+>", match =>
        {
            var tagContent = match.Value;
            var tagNameMatch = Regex.Match(tagContent, @"^</?\s*([a-z0-9]+)", RegexOptions.IgnoreCase);
            if (!tagNameMatch.Success) return string.Empty;
            var tagName = tagNameMatch.Groups[1].Value.ToLowerInvariant();
            if (!AllowedTags.Contains(tagName)) return string.Empty;
            if (AllowedAttributes.Count == 0)
            {
                if (tagContent.StartsWith("</")) return $"</{tagName}>";
                if (tagContent.EndsWith("/>")) return $"<{tagName} />";
                return $"<{tagName}>";
            }
            return match.Value;
        });
        return withoutScript.Trim();
    }
}
