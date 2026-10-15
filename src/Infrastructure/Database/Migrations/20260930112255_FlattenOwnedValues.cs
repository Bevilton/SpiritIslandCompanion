using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <summary>
    /// A change to the model only — the schema is exactly as it was. A game's result stops
    /// being an owned entity (tracked by the game's key) and becomes a complex property, which
    /// is what a value with several parts is; and the user's expansions move from a settings
    /// wrapper onto the user itself. Both were already stored as columns of the owning row, so
    /// there is nothing to run; the migration exists to keep the snapshot in step.
    /// </summary>
    public partial class FlattenOwnedValues : Migration
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
