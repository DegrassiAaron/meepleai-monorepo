using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <summary>
    /// Migration: Add support for message editing and deletion (CHAT-06)
    ///
    /// Changes:
    /// - Adds user_id column to track message ownership
    /// - Adds edit tracking fields (updated_at)
    /// - Adds soft delete fields (is_deleted, deleted_at, deleted_by_user_id)
    /// - Adds invalidation flag (is_invalidated) for cascade invalidation
    /// - Adds sequence_number for message ordering
    /// - Creates indexes for performance
    /// - Adds check constraints for data integrity
    /// </summary>
    public partial class AddChatMessageEditDeleteSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add new columns (all nullable initially for safe backfill)
            migrationBuilder.AddColumn<string>(
                name: "user_id",
                table: "chat_logs",
                type: "text",
                nullable: true,
                maxLength: 64);

            migrationBuilder.AddColumn<DateTime?>(
                name: "updated_at",
                table: "chat_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "chat_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime?>(
                name: "deleted_at",
                table: "chat_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deleted_by_user_id",
                table: "chat_logs",
                type: "text",
                nullable: true,
                maxLength: 64);

            migrationBuilder.AddColumn<bool>(
                name: "is_invalidated",
                table: "chat_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "sequence_number",
                table: "chat_logs",
                type: "integer",
                nullable: true); // Nullable initially for backfill

            // Step 2: Add foreign key constraints
            migrationBuilder.AddForeignKey(
                name: "fk_chat_logs_user_id",
                table: "chat_logs",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_chat_logs_deleted_by_user_id",
                table: "chat_logs",
                column: "deleted_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // Step 3: Backfill user_id for existing user messages
            // User messages (Level = "user") get user_id from chats.user_id
            // AI messages (Level = "assistant") remain with user_id = NULL
            migrationBuilder.Sql(@"
                UPDATE chat_logs cl
                SET user_id = c.user_id
                FROM chats c
                WHERE cl.chat_id = c.id
                  AND cl.""Level"" = 'user'
                  AND cl.user_id IS NULL;
            ");

            // Step 4: Backfill sequence_number based on created_at ordering
            // Use window function to generate sequence within each chat
            migrationBuilder.Sql(@"
                WITH sequenced AS (
                    SELECT id, ROW_NUMBER() OVER (PARTITION BY chat_id ORDER BY created_at) - 1 AS seq
                    FROM chat_logs
                    WHERE sequence_number IS NULL
                )
                UPDATE chat_logs
                SET sequence_number = sequenced.seq
                FROM sequenced
                WHERE chat_logs.id = sequenced.id;
            ");

            // Step 5: Make sequence_number NOT NULL after backfill
            migrationBuilder.AlterColumn<int>(
                name: "sequence_number",
                table: "chat_logs",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            // Step 6: Create indexes for performance
            // Partial index for user ownership lookups (excludes AI messages with null user_id)
            migrationBuilder.CreateIndex(
                name: "idx_chat_logs_user_id",
                table: "chat_logs",
                column: "user_id",
                filter: "user_id IS NOT NULL");

            // Partial index for soft-deleted message queries (admin view)
            migrationBuilder.CreateIndex(
                name: "idx_chat_logs_deleted_at",
                table: "chat_logs",
                column: "deleted_at",
                filter: "deleted_at IS NOT NULL");

            // Composite index for invalidation queries (critical for performance)
            migrationBuilder.CreateIndex(
                name: "idx_chat_logs_chat_id_sequence_role",
                table: "chat_logs",
                columns: new[] { "chat_id", "sequence_number", "Level" });

            // Step 7: Add check constraints for data integrity
            // Constraint 1: Deleted consistency (is_deleted implies deleted_at and deleted_by_user_id are set)
            migrationBuilder.Sql(@"
                ALTER TABLE chat_logs
                ADD CONSTRAINT chk_deleted_consistency
                CHECK (
                    (is_deleted = false AND deleted_at IS NULL AND deleted_by_user_id IS NULL)
                    OR
                    (is_deleted = true AND deleted_at IS NOT NULL AND deleted_by_user_id IS NOT NULL)
                );
            ");

            // Constraint 2: Updated timestamp must be after created timestamp
            migrationBuilder.Sql(@"
                ALTER TABLE chat_logs
                ADD CONSTRAINT chk_updated_at_after_created_at
                CHECK (updated_at IS NULL OR updated_at >= created_at);
            ");

            // Step 8: Add column comments for documentation
            migrationBuilder.Sql(@"
                COMMENT ON COLUMN chat_logs.user_id IS 'User who created the message. NULL for AI-generated messages.';
                COMMENT ON COLUMN chat_logs.updated_at IS 'Timestamp of last edit. NULL if never edited.';
                COMMENT ON COLUMN chat_logs.is_deleted IS 'Soft delete flag. Deleted messages hidden from UI but retained for audit.';
                COMMENT ON COLUMN chat_logs.deleted_at IS 'Timestamp when message was soft-deleted.';
                COMMENT ON COLUMN chat_logs.deleted_by_user_id IS 'User who deleted the message (may differ from creator for admin deletions).';
                COMMENT ON COLUMN chat_logs.is_invalidated IS 'Flag indicating message is invalidated due to prior message edit/delete.';
                COMMENT ON COLUMN chat_logs.sequence_number IS 'Message ordering within chat (0-indexed). Used for invalidation logic.';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop check constraints
            migrationBuilder.Sql("ALTER TABLE chat_logs DROP CONSTRAINT IF EXISTS chk_deleted_consistency;");
            migrationBuilder.Sql("ALTER TABLE chat_logs DROP CONSTRAINT IF EXISTS chk_updated_at_after_created_at;");

            // Drop indexes
            migrationBuilder.DropIndex(
                name: "idx_chat_logs_user_id",
                table: "chat_logs");

            migrationBuilder.DropIndex(
                name: "idx_chat_logs_deleted_at",
                table: "chat_logs");

            migrationBuilder.DropIndex(
                name: "idx_chat_logs_chat_id_sequence_role",
                table: "chat_logs");

            // Drop foreign keys
            migrationBuilder.DropForeignKey(
                name: "fk_chat_logs_user_id",
                table: "chat_logs");

            migrationBuilder.DropForeignKey(
                name: "fk_chat_logs_deleted_by_user_id",
                table: "chat_logs");

            // Drop columns
            migrationBuilder.DropColumn(
                name: "user_id",
                table: "chat_logs");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "chat_logs");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "chat_logs");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "chat_logs");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_id",
                table: "chat_logs");

            migrationBuilder.DropColumn(
                name: "is_invalidated",
                table: "chat_logs");

            migrationBuilder.DropColumn(
                name: "sequence_number",
                table: "chat_logs");
        }
    }
}
