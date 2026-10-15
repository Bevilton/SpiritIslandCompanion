using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <summary>
    /// Three owned parts of aggregates stop carrying identities they never used: a game's result
    /// and a user's settings lose their id columns, and the scenario a game was played under moves
    /// from its own one-row-per-game table onto the game itself. Written by hand rather than as
    /// scaffolded — the scaffolder read the change as a rename of the result's id column into the
    /// scenario column, which would have dropped every recorded scenario.
    /// </summary>
    public partial class RemoveOwnedPartIdentities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ScenarioId_Value",
                table: "Games",
                type: "nvarchar(max)",
                nullable: true);

            // The scenario travels with the game before its old table goes.
            migrationBuilder.Sql("""
                UPDATE g SET g.[ScenarioId_Value] = s.[ScenarioId_Value]
                FROM [Games] g
                INNER JOIN [Game_PlayedScenario] s ON s.[GameId] = g.[Id];
                """);

            migrationBuilder.DropTable(
                name: "Game_PlayedScenario");

            migrationBuilder.DropColumn(
                name: "Result_Id_Value",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "UserSettings_Id_Value",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The ids were never referenced, so any fresh value restores the old shape. A finished
            // game gets one, a draft stays without — the old model read the result as absent when
            // its id was.
            migrationBuilder.AddColumn<Guid>(
                name: "Result_Id_Value",
                table: "Games",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql("UPDATE [Games] SET [Result_Id_Value] = NEWID() WHERE [Result_Win] IS NOT NULL;");

            migrationBuilder.AddColumn<Guid>(
                name: "UserSettings_Id_Value",
                table: "Users",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("UPDATE [Users] SET [UserSettings_Id_Value] = NEWID();");

            migrationBuilder.CreateTable(
                name: "Game_PlayedScenario",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id_Value = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScenarioId_Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Game_PlayedScenario", x => x.GameId);
                    table.ForeignKey(
                        name: "FK_Game_PlayedScenario_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql("""
                INSERT INTO [Game_PlayedScenario] ([GameId], [Id_Value], [ScenarioId_Value])
                SELECT [Id], NEWID(), [ScenarioId_Value] FROM [Games] WHERE [ScenarioId_Value] IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "ScenarioId_Value",
                table: "Games");
        }
    }
}
