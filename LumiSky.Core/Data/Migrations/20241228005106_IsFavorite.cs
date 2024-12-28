using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LumiSky.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class IsFavorite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "Timelapses",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "RawImages",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "PanoramaTimelapses",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "Panoramas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "Images",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "Timelapses");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "RawImages");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "PanoramaTimelapses");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "Panoramas");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "Images");
        }
    }
}
