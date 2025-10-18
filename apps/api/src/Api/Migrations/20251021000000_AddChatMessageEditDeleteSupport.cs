using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChatMessageEditDeleteSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_rule_specs_GameId",
                table: "rule_specs");

            migrationBuilder.DropIndex(
                name: "IX_rule_atoms_RuleSpecId",
                table: "rule_atoms");

            migrationBuilder.DropIndex(
                name: "IX_pdf_documents_GameId",
                table: "pdf_documents");

            migrationBuilder.DropIndex(
                name: "IX_chats_GameId",
                table: "chats");

            migrationBuilder.DropIndex(
                name: "IX_chat_logs_ChatId",
                table: "chat_logs");

            migrationBuilder.DropIndex(
                name: "IX_agents_GameId",
                table: "agents");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAt",
                table: "user_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAt",
                table: "user_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GameEntityId",
                table: "rule_specs",
                type: "character varying(64)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessingProgressJson",
                table: "pdf_documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastMessageAt",
                table: "chats",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "chats",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "chat_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedByUserId",
                table: "chat_logs",
                type: "character varying(64)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "chat_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsInvalidated",
                table: "chat_logs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SequenceNumber",
                table: "chat_logs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "chat_logs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "chat_logs",
                type: "character varying(64)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TokenCount",
                table: "ai_request_logs",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApiKeyId",
                table: "ai_request_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompletionTokens",
                table: "ai_request_logs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FinishReason",
                table: "ai_request_logs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "ai_request_logs",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PromptTokens",
                table: "ai_request_logs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "api_keys",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    KeyName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    KeyHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    KeyPrefix = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Scopes = table.Column<string[]>(type: "text[]", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedBy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Metadata = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_keys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_api_keys_users_RevokedBy",
                        column: x => x.RevokedBy,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_api_keys_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cache_stats",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    game_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    question_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    hit_count = table.Column<long>(type: "bigint", nullable: false),
                    miss_count = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_hit_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cache_stats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_reset_tokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_password_reset_tokens_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "prompt_templates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompt_templates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prompt_templates_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rulespec_comments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AtomId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CommentText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rulespec_comments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rulespec_comments_games_GameId",
                        column: x => x.GameId,
                        principalTable: "games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_rulespec_comments_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prompt_versions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TemplateId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Metadata = table.Column<string>(type: "character varying(4096)", maxLength: 4096, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompt_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prompt_versions_prompt_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "prompt_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_prompt_versions_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prompt_audit_logs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TemplateId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    VersionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ChangedByUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Details = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompt_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prompt_audit_logs_prompt_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "prompt_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_prompt_audit_logs_prompt_versions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "prompt_versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_prompt_audit_logs_users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rule_specs_GameEntityId",
                table: "rule_specs",
                column: "GameEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_chats_UserId_LastMessageAt",
                table: "chats",
                columns: new[] { "UserId", "LastMessageAt" });

            migrationBuilder.CreateIndex(
                name: "idx_chat_logs_chat_id_sequence_role",
                table: "chat_logs",
                columns: new[] { "ChatId", "SequenceNumber", "Level" });

            migrationBuilder.CreateIndex(
                name: "idx_chat_logs_deleted_at",
                table: "chat_logs",
                column: "DeletedAt",
                filter: "deleted_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "idx_chat_logs_user_id",
                table: "chat_logs",
                column: "UserId",
                filter: "user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_chat_logs_DeletedByUserId",
                table: "chat_logs",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_IsActive_ExpiresAt",
                table: "api_keys",
                columns: new[] { "IsActive", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_KeyHash",
                table: "api_keys",
                column: "KeyHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_RevokedBy",
                table: "api_keys",
                column: "RevokedBy");

            migrationBuilder.CreateIndex(
                name: "IX_api_keys_UserId",
                table: "api_keys",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_ExpiresAt",
                table: "password_reset_tokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_TokenHash",
                table: "password_reset_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_password_reset_tokens_UserId",
                table: "password_reset_tokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_prompt_audit_logs_Action",
                table: "prompt_audit_logs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_prompt_audit_logs_ChangedAt",
                table: "prompt_audit_logs",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_prompt_audit_logs_ChangedByUserId",
                table: "prompt_audit_logs",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_prompt_audit_logs_TemplateId",
                table: "prompt_audit_logs",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_prompt_audit_logs_VersionId",
                table: "prompt_audit_logs",
                column: "VersionId");

            migrationBuilder.CreateIndex(
                name: "IX_prompt_templates_Category",
                table: "prompt_templates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_prompt_templates_CreatedAt",
                table: "prompt_templates",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_prompt_templates_CreatedByUserId",
                table: "prompt_templates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_prompt_templates_Name",
                table: "prompt_templates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prompt_versions_CreatedAt",
                table: "prompt_versions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_prompt_versions_CreatedByUserId",
                table: "prompt_versions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_prompt_versions_TemplateId_IsActive",
                table: "prompt_versions",
                columns: new[] { "TemplateId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_prompt_versions_TemplateId_VersionNumber",
                table: "prompt_versions",
                columns: new[] { "TemplateId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rulespec_comments_AtomId",
                table: "rulespec_comments",
                column: "AtomId");

            migrationBuilder.CreateIndex(
                name: "IX_rulespec_comments_GameId_Version",
                table: "rulespec_comments",
                columns: new[] { "GameId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_rulespec_comments_UserId",
                table: "rulespec_comments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_chat_logs_users_DeletedByUserId",
                table: "chat_logs",
                column: "DeletedByUserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_chat_logs_users_UserId",
                table: "chat_logs",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_chats_users_UserId",
                table: "chats",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_rule_specs_games_GameEntityId",
                table: "rule_specs",
                column: "GameEntityId",
                principalTable: "games",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_chat_logs_users_DeletedByUserId",
                table: "chat_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_chat_logs_users_UserId",
                table: "chat_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_chats_users_UserId",
                table: "chats");

            migrationBuilder.DropForeignKey(
                name: "FK_rule_specs_games_GameEntityId",
                table: "rule_specs");

            migrationBuilder.DropTable(
                name: "api_keys");

            migrationBuilder.DropTable(
                name: "cache_stats");

            migrationBuilder.DropTable(
                name: "password_reset_tokens");

            migrationBuilder.DropTable(
                name: "prompt_audit_logs");

            migrationBuilder.DropTable(
                name: "rulespec_comments");

            migrationBuilder.DropTable(
                name: "prompt_versions");

            migrationBuilder.DropTable(
                name: "prompt_templates");

            migrationBuilder.DropIndex(
                name: "IX_rule_specs_GameEntityId",
                table: "rule_specs");

            migrationBuilder.DropIndex(
                name: "IX_chats_UserId_LastMessageAt",
                table: "chats");

            migrationBuilder.DropIndex(
                name: "idx_chat_logs_chat_id_sequence_role",
                table: "chat_logs");

            migrationBuilder.DropIndex(
                name: "idx_chat_logs_deleted_at",
                table: "chat_logs");

            migrationBuilder.DropIndex(
                name: "idx_chat_logs_user_id",
                table: "chat_logs");

            migrationBuilder.DropIndex(
                name: "IX_chat_logs_DeletedByUserId",
                table: "chat_logs");

            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "user_sessions");

            migrationBuilder.DropColumn(
                name: "GameEntityId",
                table: "rule_specs");

            migrationBuilder.DropColumn(
                name: "ProcessingProgressJson",
                table: "pdf_documents");

            migrationBuilder.DropColumn(
                name: "LastMessageAt",
                table: "chats");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "chats");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "chat_logs");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "chat_logs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "chat_logs");

            migrationBuilder.DropColumn(
                name: "IsInvalidated",
                table: "chat_logs");

            migrationBuilder.DropColumn(
                name: "SequenceNumber",
                table: "chat_logs");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "chat_logs");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "chat_logs");

            migrationBuilder.DropColumn(
                name: "ApiKeyId",
                table: "ai_request_logs");

            migrationBuilder.DropColumn(
                name: "CompletionTokens",
                table: "ai_request_logs");

            migrationBuilder.DropColumn(
                name: "FinishReason",
                table: "ai_request_logs");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "ai_request_logs");

            migrationBuilder.DropColumn(
                name: "PromptTokens",
                table: "ai_request_logs");

            migrationBuilder.AlterColumn<int>(
                name: "TokenCount",
                table: "ai_request_logs",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_rule_specs_GameId",
                table: "rule_specs",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_rule_atoms_RuleSpecId",
                table: "rule_atoms",
                column: "RuleSpecId");

            migrationBuilder.CreateIndex(
                name: "IX_pdf_documents_GameId",
                table: "pdf_documents",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_chats_GameId",
                table: "chats",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_logs_ChatId",
                table: "chat_logs",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_agents_GameId",
                table: "agents",
                column: "GameId");
        }
    }
}
