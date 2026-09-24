using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PtcgpTracker.Api.Services;

public static partial class ShareHandles
{
    private const int MaxLength = 30;
    private const string Fallback = "trainer";

    /// <summary>
    /// Turns a display name into a lowercase, URL-safe slug ("Test Trainer" → "test-trainer").
    /// Accents are stripped; anything else non-alphanumeric collapses into a single hyphen.
    /// </summary>
    public static string Slugify(string displayName)
    {
        var decomposed = displayName.Normalize(NormalizationForm.FormD);
        var withoutMarks = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                withoutMarks.Append(c);
            }
        }

        var slug = NonAlphanumericRun()
            .Replace(withoutMarks.ToString().ToLowerInvariant(), "-")
            .Trim('-');

        if (slug.Length > MaxLength)
        {
            slug = slug[..MaxLength].TrimEnd('-');
        }

        return slug.Length == 0 ? Fallback : slug;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonAlphanumericRun();
}
