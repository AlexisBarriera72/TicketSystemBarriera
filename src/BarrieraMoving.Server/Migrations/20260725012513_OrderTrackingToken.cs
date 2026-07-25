using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarrieraMoving.Server.Migrations
{
    /// <inheritdoc />
    public partial class OrderTrackingToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TrackingToken",
                table: "Orders",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TrackingTokenCreatedUtc",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TrackingToken",
                table: "Orders",
                column: "TrackingToken",
                unique: true,
                filter: "[TrackingToken] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_TrackingToken",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TrackingToken",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TrackingTokenCreatedUtc",
                table: "Orders");
        }
    }
}
