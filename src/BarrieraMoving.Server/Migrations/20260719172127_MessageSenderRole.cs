using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BarrieraMoving.Server.Migrations
{
    /// <inheritdoc />
    public partial class MessageSenderRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                table: "Messages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SenderRole",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: true);

            // Backfill: los mensajes de sistema existentes se detectaban por el
            // prefijo "[SISTEMA]"/"[EVENTO]" — marcarlos con el flag real.
            // Ojo: '[' es comodín en LIKE de SQL Server; se escapa como '[[]'.
            migrationBuilder.Sql("UPDATE Messages SET IsSystem = 1 WHERE Content LIKE '[[]%'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSystem",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "SenderRole",
                table: "Messages");
        }
    }
}
