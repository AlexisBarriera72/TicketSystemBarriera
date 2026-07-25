using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarrieraMoving.Server.Migrations
{
    /// <inheritdoc />
    public partial class GuestReferenceCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReferenceCode",
                table: "QuoteRequests",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_ReferenceCode",
                table: "QuoteRequests",
                column: "ReferenceCode",
                unique: true,
                filter: "[ReferenceCode] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_QuoteRequests_ReferenceCode",
                table: "QuoteRequests");

            migrationBuilder.DropColumn(
                name: "ReferenceCode",
                table: "QuoteRequests");
        }
    }
}
