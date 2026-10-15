using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <summary>
    /// Brings every stored address into the canonical form <c>Email.Create</c> now produces —
    /// trimmed and lower-cased — so the exact-match lookups the handlers do find accounts that
    /// were registered before the form was fixed. No schema change; the unique index on the
    /// column cannot collide, since SQL Server's default collation already treated two spellings
    /// of one address as the same value.
    /// </summary>
    public partial class NormalizeEmailCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE [Users] SET [Email] = LOWER(LTRIM(RTRIM([Email])));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The original casing is not recoverable, and the lower-cased form is a valid address.
        }
    }
}
