using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <summary>
    /// A change to the model only — the schema is exactly as it was. A game's result stops
    /// copying the fields of the outcome it was scored from and holds the outcome itself, as a
    /// complex property nested in the result's. The outcome's columns keep the names they had
    /// (see <c>GameConfiguration</c>), so there is nothing to run; the migration exists to keep
    /// the snapshot in step.
    /// </summary>
    public partial class NestGameOutcome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
