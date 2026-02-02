using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ErenTravel3API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Emri = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Packages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Paketa = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Kohezgjatja = table.Column<int>(type: "int", nullable: false),
                    Cmimi = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    Pershkrimi = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsAvailable = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Reservations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    PackageId = table.Column<int>(type: "int", nullable: false),
                    DataNisjes = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    DataKthimit = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    NrPersonave = table.Column<int>(type: "int", nullable: false),
                    NrDhomave = table.Column<int>(type: "int", nullable: false),
                    Cmimi = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservations_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reservations_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Packages",
                columns: new[] { "Id", "Cmimi", "IsAvailable", "Kohezgjatja", "Paketa", "Pershkrimi" },
                values: new object[,]
                {
                    { 1, 299m, true, 5, "Budapest – Hungari", "Fluturim + Hotel • Nisja nga Tirana" },
                    { 2, 305m, true, 4, "Pragë – Çeki", "Hotel 4★ • Vizita panoramike" },
                    { 3, 699m, true, 5, "Dubai – Emiratet", "Guide • Hotel 4★ • Safari në shkretëtirë" },
                    { 4, 399m, true, 4, "Paris – Francë", "Guidë • Hotel • Qyteti i dritave" },
                    { 5, 499m, true, 5, "DisneyLand Paris – Francë", "Guide • Hotel 4★ • Argëtim familjar" },
                    { 6, 344m, true, 4, "Venecia, Verona – Itali", "Guide • Hotel 4★ • Qytetet romantike" },
                    { 7, 99m, true, 2, "Firence – Itali", "Hotel 3★ • Arti dhe kultura" },
                    { 8, 179m, true, 3, "Bruge – Belgjikë", "Fluturim + Hotel + Mëngjes" },
                    { 9, 149m, true, 4, "Stockholm – Suedi", "Fluturim + Hotel • Kryeqyteti skandinav" },
                    { 10, 419m, true, 4, "Strasburg - Francë • Zyrih - Zvicërr", "Guide • Hotel 4★ • Dy vende" },
                    { 11, 699m, true, 7, "Arabi Saudite", "Guide • Hotel 4★ • Eksplorim kulturor" },
                    { 12, 1199m, true, 6, "Jordani", "Guide • Hotel 4★ • Petra dhe më shumë" },
                    { 13, 175m, true, 4, "Bukuresht – Rumani", "Fluturim + Hotel 3★" },
                    { 14, 399m, true, 4, "Barcelonë – Spanjë", "Fluturim + Hotel • Oferta fundviti" },
                    { 15, 135m, true, 3, "Alberobello – Itali", "Fluturim + Hotel • Fshati karakteristik" },
                    { 16, 249m, true, 3, "Korçë – Shqipëri", "Hotel Kocibelli • Viti i Ri tradicional" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CustomerId",
                table: "Reservations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_PackageId",
                table: "Reservations",
                column: "PackageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reservations");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Packages");
        }
    }
}
