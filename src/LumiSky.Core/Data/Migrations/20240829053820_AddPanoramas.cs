using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LumiSky.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPanoramas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Panoramas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedOn = table.Column<DateTime>(type: "DATETIME", nullable: false),
                    Filename = table.Column<string>(type: "TEXT COLLATE NOCASE", nullable: false),
                    ExposedOn = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Panoramas", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Panoramas_ExposedOn",
                table: "Panoramas",
                column: "ExposedOn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Panoramas");
        }
    }
}
