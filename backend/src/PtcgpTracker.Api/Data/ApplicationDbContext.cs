using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PtcgpTracker.Api.Data.Entities;

namespace PtcgpTracker.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<CollectionEntry> CollectionEntries => Set<CollectionEntry>();

    public DbSet<TradeListEntry> TradeListEntries => Set<TradeListEntry>();

    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<CollectionEntry>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.CardId }).IsUnique();
        });

        builder.Entity<TradeListEntry>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.CardId, e.Direction }).IsUnique();
        });

        builder.Entity<AppSetting>(entity =>
        {
            entity.HasKey(e => e.Key);
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.HasIndex(e => e.TradeListShareHandle).IsUnique();
        });
    }
}
