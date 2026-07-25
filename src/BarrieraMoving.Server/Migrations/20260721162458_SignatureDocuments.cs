using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarrieraMoving.Server.Migrations
{
    /// <inheritdoc />
    public partial class SignatureDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SignatureDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsProvisional = table.Column<bool>(type: "bit", nullable: false),
                    ProviderEnvelopeId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SignerName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SignerEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SignedCapturedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PdfPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailStatus = table.Column<int>(type: "int", nullable: false),
                    EmailError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignatureDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SignatureDocuments_AspNetUsers_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SignatureDocuments_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SignatureDocuments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SignatureDocuments_IdempotencyKey",
                table: "SignatureDocuments",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SignatureDocuments_OrderId",
                table: "SignatureDocuments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SignatureDocuments_ProviderEnvelopeId",
                table: "SignatureDocuments",
                column: "ProviderEnvelopeId",
                unique: true,
                filter: "[ProviderEnvelopeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SignatureDocuments_RequestedByUserId",
                table: "SignatureDocuments",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SignatureDocuments_ReviewedByUserId",
                table: "SignatureDocuments",
                column: "ReviewedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignatureDocuments");
        }
    }
}
