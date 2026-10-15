using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <summary>
    /// The single-value value objects stop being owned entities and become plain converted
    /// columns, so every <c>X_Value</c> column is renamed to <c>X</c>; the two text columns
    /// whose value objects have a limit get it as their width; and a user's expansions move
    /// from a table of their own into one JSON array on the user's row.
    /// <para>
    /// Written by hand rather than as scaffolded: the scaffolder read the change as drops and
    /// adds where it could not pair the columns (every local player's name, every extra board),
    /// paired the ones it could partly crosswise (difficulty with its modifier, score with
    /// its modifier), and dropped the expansions table without carrying its rows across.
    /// </para>
    /// </summary>
    public partial class MapValueObjectsAsColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            void Rename(string table, string from, string to) =>
                migrationBuilder.RenameColumn(name: from, table: table, newName: to);

            Rename("Games", "OwnerId_Value", "OwnerId");
            Rename("Games", "ScenarioId_Value", "ScenarioId");
            Rename("Games", "IslandSetupId_Value", "IslandSetupId");
            Rename("Games", "IslandLayout_Value", "IslandLayout");
            Rename("Games", "CustomLayoutId_Value", "CustomLayoutId");
            Rename("Games", "ExtraBoard_Value", "ExtraBoard");
            Rename("Games", "Difficulty_Value", "Difficulty");
            Rename("Games", "DifficultyModifier_Value", "DifficultyModifier");
            Rename("Games", "Note_Value", "Note");
            Rename("Games", "Result_Blight_Value", "Result_Blight");
            Rename("Games", "Result_Cards_Value", "Result_Cards");
            Rename("Games", "Result_Dahan_Value", "Result_Dahan");
            Rename("Games", "Result_Score_Value", "Result_Score");
            Rename("Games", "Result_ScoreModifier_Value", "Result_ScoreModifier");

            Rename("Game_GamePlayer", "SpiritId_Value", "SpiritId");
            Rename("Game_GamePlayer", "AspectId_Value", "AspectId");
            Rename("Game_GamePlayer", "StartingBoard_Value", "StartingBoard");
            Rename("Game_GamePlayer", "UserId_Value", "UserId");
            Rename("Game_GamePlayer", "PlayerId_Value", "PlayerId");

            Rename("Game_PlayedAdversary", "AdversaryId_Value", "AdversaryId");
            Rename("Game_PlayedAdversary", "Level_Value", "Level");

            Rename("Players", "Name_Value", "Name");
            Rename("Players", "CreatedBy_Value", "CreatedBy");

            Rename("Users", "Nickname_Value", "Nickname");

            Rename("CustomIslandLayouts", "OwnerId_Value", "OwnerId");
            Rename("CustomIslandLayouts", "Name_Value", "Name");
            Rename("CustomIslandLayouts", "Geometry_Value", "Geometry");

            // The domain has always refused longer values; the columns now say so too.
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Players",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Games",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            // The expansions travel onto the user's row before their old table goes: one JSON
            // array of ids, in the order they were stored, and an empty array for an account
            // that never opened Settings.
            migrationBuilder.AddColumn<string>(
                name: "Expansions",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.Sql("""
                UPDATE u SET u.[Expansions] = ISNULL((
                    SELECT '[' + STRING_AGG('"' + STRING_ESCAPE(e.[Value], 'json') + '"', ',') WITHIN GROUP (ORDER BY e.[Id]) + ']'
                    FROM [ExpansionId] e
                    WHERE e.[UserSettingsUserId] = u.[Id]), '[]')
                FROM [Users] u;
                """);

            migrationBuilder.DropTable(
                name: "ExpansionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            void Rename(string table, string from, string to) =>
                migrationBuilder.RenameColumn(name: from, table: table, newName: to);

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

            // Back into rows, in the array's order — the identity column numbers them as they arrive.
            migrationBuilder.Sql("""
                INSERT INTO [ExpansionId] ([UserSettingsUserId], [Value])
                SELECT u.[Id], j.[value]
                FROM [Users] u
                CROSS APPLY OPENJSON(u.[Expansions]) j
                ORDER BY u.[Id], CAST(j.[key] AS int);
                """);

            migrationBuilder.DropColumn(
                name: "Expansions",
                table: "Users");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Players",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Games",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            Rename("Games", "OwnerId", "OwnerId_Value");
            Rename("Games", "ScenarioId", "ScenarioId_Value");
            Rename("Games", "IslandSetupId", "IslandSetupId_Value");
            Rename("Games", "IslandLayout", "IslandLayout_Value");
            Rename("Games", "CustomLayoutId", "CustomLayoutId_Value");
            Rename("Games", "ExtraBoard", "ExtraBoard_Value");
            Rename("Games", "Difficulty", "Difficulty_Value");
            Rename("Games", "DifficultyModifier", "DifficultyModifier_Value");
            Rename("Games", "Note", "Note_Value");
            Rename("Games", "Result_Blight", "Result_Blight_Value");
            Rename("Games", "Result_Cards", "Result_Cards_Value");
            Rename("Games", "Result_Dahan", "Result_Dahan_Value");
            Rename("Games", "Result_Score", "Result_Score_Value");
            Rename("Games", "Result_ScoreModifier", "Result_ScoreModifier_Value");

            Rename("Game_GamePlayer", "SpiritId", "SpiritId_Value");
            Rename("Game_GamePlayer", "AspectId", "AspectId_Value");
            Rename("Game_GamePlayer", "StartingBoard", "StartingBoard_Value");
            Rename("Game_GamePlayer", "UserId", "UserId_Value");
            Rename("Game_GamePlayer", "PlayerId", "PlayerId_Value");

            Rename("Game_PlayedAdversary", "AdversaryId", "AdversaryId_Value");
            Rename("Game_PlayedAdversary", "Level", "Level_Value");

            Rename("Players", "Name", "Name_Value");
            Rename("Players", "CreatedBy", "CreatedBy_Value");

            Rename("Users", "Nickname", "Nickname_Value");

            Rename("CustomIslandLayouts", "OwnerId", "OwnerId_Value");
            Rename("CustomIslandLayouts", "Name", "Name_Value");
            Rename("CustomIslandLayouts", "Geometry", "Geometry_Value");
        }
    }
}
