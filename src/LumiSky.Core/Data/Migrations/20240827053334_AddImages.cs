using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LumiSky.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedOn = table.Column<DateTime>(type: "DATETIME", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Filename = table.Column<string>(type: "TEXT COLLATE NOCASE", nullable: false),
                    ExposedOn = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RawImages",
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
                    table.PrimaryKey("PK_RawImages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Images_ExposedOn",
                table: "Images",
                column: "ExposedOn");

            migrationBuilder.CreateIndex(
                name: "IX_RawImages_ExposedOn",
                table: "RawImages",
                column: "ExposedOn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Images");

            migrationBuilder.DropTable(
                name: "RawImages");
        }
    }
}
