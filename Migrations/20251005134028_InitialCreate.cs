using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecipeUI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alapanyagok",
                columns: table => new
                {
                    AlapanyagID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AlapanyagNev = table.Column<string>(type: "TEXT", nullable: false),
                    Mertekegyseg = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alapanyagok", x => x.AlapanyagID);
                });

            migrationBuilder.CreateTable(
                name: "Receptek",
                columns: table => new
                {
                    ReceptID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReceptNev = table.Column<string>(type: "TEXT", nullable: false),
                    Elkeszites = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receptek", x => x.ReceptID);
                });

            migrationBuilder.CreateTable(
                name: "Raktarak",
                columns: table => new
                {
                    AlapanyagID = table.Column<int>(type: "INTEGER", nullable: false),
                    Mennyiseg = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Raktarak", x => x.AlapanyagID);
                    table.ForeignKey(
                        name: "FK_Raktarak_Alapanyagok_AlapanyagID",
                        column: x => x.AlapanyagID,
                        principalTable: "Alapanyagok",
                        principalColumn: "AlapanyagID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReceptHozzavalok",
                columns: table => new
                {
                    ReceptID = table.Column<int>(type: "INTEGER", nullable: false),
                    AlapanyagID = table.Column<int>(type: "INTEGER", nullable: false),
                    SzükségesMennyiseg = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceptHozzavalok", x => new { x.ReceptID, x.AlapanyagID });
                    table.ForeignKey(
                        name: "FK_ReceptHozzavalok_Alapanyagok_AlapanyagID",
                        column: x => x.AlapanyagID,
                        principalTable: "Alapanyagok",
                        principalColumn: "AlapanyagID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReceptHozzavalok_Receptek_ReceptID",
                        column: x => x.ReceptID,
                        principalTable: "Receptek",
                        principalColumn: "ReceptID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alapanyagok_AlapanyagNev",
                table: "Alapanyagok",
                column: "AlapanyagNev",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReceptHozzavalok_AlapanyagID",
                table: "ReceptHozzavalok",
                column: "AlapanyagID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Raktarak");

            migrationBuilder.DropTable(
                name: "ReceptHozzavalok");

            migrationBuilder.DropTable(
                name: "Alapanyagok");

            migrationBuilder.DropTable(
                name: "Receptek");
        }
    }
}
