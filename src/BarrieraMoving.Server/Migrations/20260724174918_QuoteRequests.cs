using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarrieraMoving.Server.Migrations
{
    /// <inheritdoc />
    public partial class QuoteRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuoteRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginZone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DestinationZone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreferredDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Handled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuoteRequests", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuoteRequests");
        }
    }
}
