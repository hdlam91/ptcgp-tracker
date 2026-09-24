namespace PtcgpTracker.Api.Data.Entities;

/// <summary>Admin-controlled, app-wide key/value setting (e.g. whether registration is open).</summary>
public class AppSetting
{
    public required string Key { get; set; }

    public required string Value { get; set; }
}
