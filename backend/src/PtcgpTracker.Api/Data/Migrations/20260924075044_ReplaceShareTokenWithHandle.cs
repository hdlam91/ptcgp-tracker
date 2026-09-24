using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PtcgpTracker.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceShareTokenWithHandle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TradeListShareToken",
                table: "AspNetUsers",
                newName: "TradeListShareHandle");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_TradeListShareToken",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_TradeListShareHandle");

            // Old values are random hex tokens, not name-derived handles; their /shared/{token}
            // links no longer resolve anyway, so turn sharing off and let users re-enable it.
            migrationBuilder.Sql("UPDATE \"AspNetUsers\" SET \"TradeListShareHandle\" = NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TradeListShareHandle",
                table: "AspNetUsers",
                newName: "TradeListShareToken");

            migrationBuilder.RenameIndex(
                name: "IX_AspNetUsers_TradeListShareHandle",
                table: "AspNetUsers",
                newName: "IX_AspNetUsers_TradeListShareToken");
        }
    }
}
