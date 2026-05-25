using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Viagem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserTravelDestinations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    PlaceId = table.Column<int>(type: "INTEGER", nullable: true),
                    PlaceName = table.Column<string>(type: "TEXT", nullable: false),
                    Latitude = table.Column<string>(type: "TEXT", nullable: true),
                    Longitude = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTravelDestinations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTravelDestinations_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserTravelStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    TripCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DestinationCount = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalDays = table.Column<int>(type: "INTEGER", nullable: false),
                    TransportationStatsJson = table.Column<string>(type: "TEXT", nullable: false),
                    TransportationTotalTrips = table.Column<int>(type: "INTEGER", nullable: false),
                    TransportationTotalHours = table.Column<double>(type: "REAL", nullable: false),
                    TransportationAvgHours = table.Column<double>(type: "REAL", nullable: false),
                    LodgingStatsJson = table.Column<string>(type: "TEXT", nullable: false),
                    LodgingTotalNights = table.Column<int>(type: "INTEGER", nullable: false),
                    ActivityTotalCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ActivityAvgPerTrip = table.Column<double>(type: "REAL", nullable: false),
                    ExpenseStatsJson = table.Column<string>(type: "TEXT", nullable: false),
                    ExpenseCurrencyCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTravelStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTravelStats_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserTravelDestinations_PlaceId",
                table: "UserTravelDestinations",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTravelDestinations_UserId_PlaceId",
                table: "UserTravelDestinations",
                columns: new[] { "UserId", "PlaceId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserTravelStats_UserId_Year",
                table: "UserTravelStats",
                columns: new[] { "UserId", "Year" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserTravelDestinations");

            migrationBuilder.DropTable(
                name: "UserTravelStats");
        }
    }
}
