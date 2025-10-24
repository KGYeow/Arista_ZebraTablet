using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arista_ZebraTablet.Shared.Migrations
{
    /// <inheritdoc />
    public partial class CreateIdentitySchemaV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Format",
                table: "ScannedBarcode",
                newName: "BarcodeType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BarcodeType",
                table: "ScannedBarcode",
                newName: "Format");
        }
    }
}
