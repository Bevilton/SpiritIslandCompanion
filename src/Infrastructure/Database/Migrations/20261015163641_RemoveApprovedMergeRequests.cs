using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations
{
    /// <summary>
    /// An approved merge request is now removed together with the local player it was about
    /// (see <c>ApproveMergeRequest</c>), and the readers no longer filter the approved ones out —
    /// a row still carrying that status would surface on the People page as an outgoing request
    /// about a guest who no longer exists. Nothing ever read one back. No schema change.
    /// </summary>
    public partial class RemoveApprovedMergeRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [PlayerMergeRequests] WHERE [Status] = 'Approved';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The removed rows said nothing anyone could act on, and the model before this
            // migration filtered them out of every reader.
        }
    }
}
