namespace PtcgpTracker.Api.Models;

public record AdminUserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    DateTimeOffset CreatedAt,
    bool IsAdmin,
    int OwnedUniqueCards,
    int WantCount,
    int OfferCount,
    string? ShareHandle);

public record SetAdminRequest(bool IsAdmin);

public record AdminSettingsResponse(bool RegistrationOpen);

public record UpdateAdminSettingsRequest(bool RegistrationOpen);

public record CatalogStatusResponse(string RepoTag, int CardCount, DateTimeOffset? LastRefreshedAt, string? LastError);

public record PublicConfigResponse(bool RegistrationOpen);
