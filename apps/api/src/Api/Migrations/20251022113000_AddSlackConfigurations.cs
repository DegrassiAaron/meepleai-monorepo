using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSlackConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "slack_configs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProjectName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProjectDescription = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ProjectUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    DocumentationUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ContactEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    WorkspaceUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Channel = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    WebhookUrlEncrypted = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_slack_configs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_slack_configs_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_slack_configs_CreatedByUserId",
                table: "slack_configs",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_slack_configs_ProjectName_Channel",
                table: "slack_configs",
                columns: new[] { "ProjectName", "Channel" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "slack_configs");
        }
    }
}
