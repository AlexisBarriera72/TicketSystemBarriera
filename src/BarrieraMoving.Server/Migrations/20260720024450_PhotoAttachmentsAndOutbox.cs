using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarrieraMoving.Server.Migrations
{
    /// <inheritdoc />
    public partial class PhotoAttachmentsAndOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClockInCapturedAtUtc",
                table: "TimeEntries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClockInIdempotencyKey",
                table: "TimeEntries",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClockOutCapturedAtUtc",
                table: "TimeEntries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClockOutIdempotencyKey",
                table: "TimeEntries",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentPath",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentThumbPath",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CapturedAtUtc",
                table: "Messages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Messages",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Messages",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Messages",
                type: "float",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_ClockInIdempotencyKey",
                table: "TimeEntries",
                column: "ClockInIdempotencyKey",
                unique: true,
                filter: "[ClockInIdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEntries_ClockOutIdempotencyKey",
                table: "TimeEntries",
                column: "ClockOutIdempotencyKey",
                unique: true,
                filter: "[ClockOutIdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_IdempotencyKey",
                table: "Messages",
                column: "IdempotencyKey",
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_ClockInIdempotencyKey",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_TimeEntries_ClockOutIdempotencyKey",
                table: "TimeEntries");

            migrationBuilder.DropIndex(
                name: "IX_Messages_IdempotencyKey",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ClockInCapturedAtUtc",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "ClockInIdempotencyKey",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "ClockOutCapturedAtUtc",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "ClockOutIdempotencyKey",
                table: "TimeEntries");

            migrationBuilder.DropColumn(
                name: "AttachmentPath",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "AttachmentThumbPath",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "CapturedAtUtc",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Messages");
        }
    }
}
