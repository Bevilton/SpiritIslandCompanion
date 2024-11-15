using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Result_Win = table.Column<bool>(type: "bit", nullable: true),
                    Result_Duration = table.Column<TimeSpan>(type: "time", nullable: true),
                    Result_Cards_Value = table.Column<int>(type: "int", nullable: true),
                    Result_TerrorLevel = table.Column<int>(type: "int", nullable: true),
                    Result_Blight_Value = table.Column<int>(type: "int", nullable: true),
                    Result_Dahan_Value = table.Column<int>(type: "int", nullable: true),
                    Result_Score_Value = table.Column<int>(type: "int", nullable: true),
                    Result_ScoreModifier_Value = table.Column<int>(type: "int", nullable: true),
                    Result_Id_Value = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IslandSetupId_Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Difficulty_Value = table.Column<int>(type: "int", nullable: false),
                    Note_Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name_Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy_Value = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email_Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nickname_Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Registered = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UserSettings_Id_Value = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Game_GamePlayer",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SpiritId_Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AspectId_Value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartingBoard_Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId_Value = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlayerId_Value = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Game_GamePlayer", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Game_GamePlayer_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Game_PlayedAdversary",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdversaryId_Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level_Value = table.Column<int>(type: "int", nullable: false),
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Game_PlayedAdversary", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Game_PlayedAdversary_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Game_PlayedScenario",
                columns: table => new
                {
                    GameId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScenarioId_Value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Id_Value = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "ExpansionId",
                columns: table => new
                {
                    UserSettingsUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpansionId", x => new { x.UserSettingsUserId, x.Id });
                    table.ForeignKey(
                        name: "FK_ExpansionId_Users_UserSettingsUserId",
                        column: x => x.UserSettingsUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Game_GamePlayer_GameId",
                table: "Game_GamePlayer",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Game_PlayedAdversary_GameId",
                table: "Game_PlayedAdversary",
                column: "GameId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpansionId");

            migrationBuilder.DropTable(
                name: "Game_GamePlayer");

            migrationBuilder.DropTable(
                name: "Game_PlayedAdversary");

            migrationBuilder.DropTable(
                name: "Game_PlayedScenario");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Games");
        }
    }
}
