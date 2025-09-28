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
                name: "Osszetevok",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nev = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Osszetevok", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Receptek",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Nev = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Receptek", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaktarTetelek",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OsszetevoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaktarTetelek", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaktarTetelek_Osszetevok_OsszetevoId",
                        column: x => x.OsszetevoId,
                        principalTable: "Osszetevok",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReceptOsszetevok",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReceptId = table.Column<int>(type: "INTEGER", nullable: false),
                    OsszetevoId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReceptOsszetevok", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReceptOsszetevok_Osszetevok_OsszetevoId",
                        column: x => x.OsszetevoId,
                        principalTable: "Osszetevok",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReceptOsszetevok_Receptek_ReceptId",
                        column: x => x.ReceptId,
                        principalTable: "Receptek",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RaktarTetelek_OsszetevoId",
                table: "RaktarTetelek",
                column: "OsszetevoId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceptOsszetevok_OsszetevoId",
                table: "ReceptOsszetevok",
                column: "OsszetevoId");

            migrationBuilder.CreateIndex(
                name: "IX_ReceptOsszetevok_ReceptId",
                table: "ReceptOsszetevok",
                column: "ReceptId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RaktarTetelek");

            migrationBuilder.DropTable(
                name: "ReceptOsszetevok");

            migrationBuilder.DropTable(
                name: "Osszetevok");

            migrationBuilder.DropTable(
                name: "Receptek");
        }
    }
}
