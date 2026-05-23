using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Connection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRoomIdNonIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Note: SQL Server does not support altering a column to drop the IDENTITY property.
            // We must drop the foreign keys, drop the primary key, recreate the column, and restore everything.

            migrationBuilder.Sql(@"
                -- 1. Drop the Foreign Key constraint from Message table
                ALTER TABLE [Message] DROP CONSTRAINT [FK_Message_Room_MessageRoomId];

                -- 2. Drop the Primary Key constraint from Room table
                ALTER TABLE [Room] DROP CONSTRAINT [PK_Room];

                -- 3. Add a new temporary column
                ALTER TABLE [Room] ADD [NewId] int NULL;

                -- 4. Copy the data
                EXEC('UPDATE [Room] SET [NewId] = [Id]');

                -- 5. Drop the old Identity column
                ALTER TABLE [Room] DROP COLUMN [Id];

                -- 6. Rename the temporary column to Id
                EXEC sp_rename 'Room.NewId', 'Id', 'COLUMN';

                -- 7. Make the new column NOT NULL
                ALTER TABLE [Room] ALTER COLUMN [Id] int NOT NULL;

                -- 8. Add the Primary Key constraint back
                ALTER TABLE [Room] ADD CONSTRAINT [PK_Room] PRIMARY KEY ([Id]);

                -- 9. Add the Foreign Key constraint back
                ALTER TABLE [Message] ADD CONSTRAINT [FK_Message_Room_MessageRoomId] FOREIGN KEY ([MessageRoomId]) REFERENCES [Room]([Id]) ON DELETE CASCADE;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "Room",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1");
        }
    }
}
