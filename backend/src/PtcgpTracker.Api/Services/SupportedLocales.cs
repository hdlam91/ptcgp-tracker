namespace PtcgpTracker.Api.Services;

/// <summary>
/// The only source of truth for which language codes the <c>POST /api/account/locale</c>
/// endpoint accepts. Keep this in sync with the frontend's own locale list
/// (<c>frontend/src/composables/useLocale.ts</c>) — adding a language means adding the code in
/// both places, plus the new message file on the frontend.
/// </summary>
public static class SupportedLocales
{
    public static readonly IReadOnlySet<string> Codes = new HashSet<string> { "en" };
}
