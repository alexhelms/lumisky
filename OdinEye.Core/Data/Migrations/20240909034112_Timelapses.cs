using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OdinEye.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class Timelapses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedOn",
                table: "RawImages",
                type: "DATETIME",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "DATETIME");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedOn",
                table: "Panoramas",
                type: "DATETIME",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "DATETIME");

            migrationBuilder.CreateTable(
                name: "Generations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedOn = table.Column<DateTime>(type: "DATETIME", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    StartedOn = table.Column<DateTime>(type: "DATETIME", nullable: true),
                    CompletedOn = table.Column<DateTime>(type: "DATETIME", nullable: true),
                    RangeBegin = table.Column<long>(type: "INTEGER", nullable: false),
                    RangeEnd = table.Column<long>(type: "INTEGER", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    Progress = table.Column<int>(type: "INTEGER", nullable: false),
                    OutputFilename = table.Column<string>(type: "TEXT COLLATE NOCASE", nullable: true),
                    JobInstanceId = table.Column<string>(type: "TEXT", nullable: true),
                    TimelapseId = table.Column<int>(type: "INTEGER", nullable: true),
                    PanoramaTimelapseId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Generations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PanoramaTimelapses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedOn = table.Column<DateTime>(type: "DATETIME", nullable: false),
                    Filename = table.Column<string>(type: "TEXT COLLATE NOCASE", nullable: false),
                    RangeBegin = table.Column<long>(type: "INTEGER", nullable: false),
                    RangeEnd = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PanoramaTimelapses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Timelapses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedOn = table.Column<DateTime>(type: "DATETIME", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Filename = table.Column<string>(type: "TEXT COLLATE NOCASE", nullable: false),
                    RangeBegin = table.Column<long>(type: "INTEGER", nullable: false),
                    RangeEnd = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timelapses", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Generations");

            migrationBuilder.DropTable(
                name: "PanoramaTimelapses");

            migrationBuilder.DropTable(
                name: "Timelapses");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedOn",
                table: "RawImages",
                type: "DATETIME",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedOn",
                table: "Panoramas",
                type: "DATETIME",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "DATETIME",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");
        }
    }
}
