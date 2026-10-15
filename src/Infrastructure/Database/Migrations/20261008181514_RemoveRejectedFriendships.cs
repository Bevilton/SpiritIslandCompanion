using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <summary>
    /// A declined friend request is now removed rather than marked, and the declined status is
    /// gone from the model — a row still carrying it would fail to read. Nothing ever read one
    /// back: the only reader was the next request between the same two people, which deleted
    /// it on its way in. No schema change.
    /// </summary>
    public partial class RemoveRejectedFriendships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [Friendships] WHERE [Status] = 'Rejected';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The removed rows said nothing anyone could act on, and the model before this
            // migration reads the remaining ones as it always did.
        }
    }
}
