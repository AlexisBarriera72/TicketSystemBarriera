using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarrieraMoving.Server.Migrations
{
    /// <inheritdoc />
    public partial class OrderClientRouteSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientEmail",
                table: "Orders",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientName",
                table: "Orders",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClientPhone",
                table: "Orders",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationZone",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginZone",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduledDate",
                table: "Orders",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientEmail",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ClientName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ClientPhone",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DestinationZone",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OriginZone",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ScheduledDate",
                table: "Orders");
        }
    }
}
