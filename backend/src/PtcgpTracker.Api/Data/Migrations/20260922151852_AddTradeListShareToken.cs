using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PtcgpTracker.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTradeListShareToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TradeListShareToken",
                table: "AspNetUsers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TradeListShareToken",
                table: "AspNetUsers",
                column: "TradeListShareToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TradeListShareToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TradeListShareToken",
                table: "AspNetUsers");
        }
    }
}
