using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Viagem.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateApplicationUserTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WebsiteAppearance",
                table: "AspNetUsers",
                newName: "ThemeColor");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ThemeColor",
                table: "AspNetUsers",
                newName: "WebsiteAppearance");
        }
    }
}
