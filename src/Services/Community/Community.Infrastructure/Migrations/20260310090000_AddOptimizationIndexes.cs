using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Community.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOptimizationIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Composite index for feed query (status + id DESC for cursor pagination)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Post_Status_Id' AND object_id = OBJECT_ID('communityPost'))
                    CREATE INDEX IX_Post_Status_Id ON communityPost(status, id DESC)
            ");

            // Composite index for feed query with createAt ordering
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Post_Status_CreateAt' AND object_id = OBJECT_ID('communityPost'))
                    CREATE INDEX IX_Post_Status_CreateAt ON communityPost(status, createAt DESC)
            ");

            // Favorite status per-user lookup
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Favorite_User_Post' AND object_id = OBJECT_ID('communityPostFavorite'))
                    CREATE INDEX IX_Favorite_User_Post ON communityPostFavorite(userId, communityPostId)
            ");

            // Save status per-user lookup
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Save_User_Post' AND object_id = OBJECT_ID('communityPostSave'))
                    CREATE INDEX IX_Save_User_Post ON communityPostSave(userId, communityPostId)
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Post_Status_Id' AND object_id = OBJECT_ID('communityPost'))
                    DROP INDEX IX_Post_Status_Id ON communityPost
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Post_Status_CreateAt' AND object_id = OBJECT_ID('communityPost'))
                    DROP INDEX IX_Post_Status_CreateAt ON communityPost
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Favorite_User_Post' AND object_id = OBJECT_ID('communityPostFavorite'))
                    DROP INDEX IX_Favorite_User_Post ON communityPostFavorite
            ");

            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Save_User_Post' AND object_id = OBJECT_ID('communityPostSave'))
                    DROP INDEX IX_Save_User_Post ON communityPostSave
            ");
        }
    }
}
