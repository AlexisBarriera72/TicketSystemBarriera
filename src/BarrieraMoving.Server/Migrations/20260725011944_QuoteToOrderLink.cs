using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarrieraMoving.Server.Migrations
{
    /// <inheritdoc />
    public partial class QuoteToOrderLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConvertedOrderId",
                table: "QuoteRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuoteRequests_ConvertedOrderId",
                table: "QuoteRequests",
                column: "ConvertedOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_QuoteRequests_Orders_ConvertedOrderId",
                table: "QuoteRequests",
                column: "ConvertedOrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QuoteRequests_Orders_ConvertedOrderId",
                table: "QuoteRequests");

            migrationBuilder.DropIndex(
                name: "IX_QuoteRequests_ConvertedOrderId",
                table: "QuoteRequests");

            migrationBuilder.DropColumn(
                name: "ConvertedOrderId",
                table: "QuoteRequests");
        }
    }
}
