using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Viagem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseSourceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceId",
                table: "Expenses",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "Expenses",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "Expenses");
        }
    }
}
