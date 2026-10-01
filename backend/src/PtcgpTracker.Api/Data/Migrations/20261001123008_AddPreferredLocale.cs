using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PtcgpTracker.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredLocale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing rows backfill to "en" (not ""), matching the entity's C# default for
            // accounts created from here on — EF's generated default only applies on insert,
            // not retroactively to rows that already existed before this column did.
            migrationBuilder.AddColumn<string>(
                name: "PreferredLocale",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "en");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredLocale",
                table: "AspNetUsers");
        }
    }
}
