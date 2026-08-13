using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomIslandLayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomLayoutId_Value",
                table: "Games",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IslandLayout_Value",
                table: "Games",
                type: "nvarchar(max)",
                maxLength: 20000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomIslandLayouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId_Value = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name_Value = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    BoardCount = table.Column<int>(type: "int", nullable: false),
                    Geometry_Value = table.Column<string>(type: "nvarchar(max)", maxLength: 20000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomIslandLayouts", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomIslandLayouts");

            migrationBuilder.DropColumn(
                name: "CustomLayoutId_Value",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "IslandLayout_Value",
                table: "Games");
        }
    }
}
