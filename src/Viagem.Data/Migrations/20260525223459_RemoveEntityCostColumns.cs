using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Viagem.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEntityCostColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CostAmount",
                table: "Transportations");

            migrationBuilder.DropColumn(
                name: "CostCurrency",
                table: "Transportations");

            migrationBuilder.DropColumn(
                name: "CostAmount",
                table: "Lodgings");

            migrationBuilder.DropColumn(
                name: "CostCurrency",
                table: "Lodgings");

            migrationBuilder.DropColumn(
                name: "CostAmount",
                table: "Activities");

            migrationBuilder.DropColumn(
                name: "CostCurrency",
                table: "Activities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CostAmount",
                table: "Transportations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostCurrency",
                table: "Transportations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostAmount",
                table: "Lodgings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostCurrency",
                table: "Lodgings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CostAmount",
                table: "Activities",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CostCurrency",
                table: "Activities",
                type: "TEXT",
                nullable: true);
        }
    }
}
