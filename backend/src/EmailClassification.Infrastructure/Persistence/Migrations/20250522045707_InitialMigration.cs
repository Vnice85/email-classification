using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EmailClassification.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "app_user",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    profile_image = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("app_user_pkey", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "email_direction",
                columns: table => new
                {
                    direction_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    direction_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("email_direction_pkey", x => x.direction_id);
                });

            migrationBuilder.CreateTable(
                name: "email_label",
                columns: table => new
                {
                    label_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    label_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("email_label_pkey", x => x.label_id);
                });

            migrationBuilder.CreateTable(
                name: "token",
                columns: table => new
                {
                    token_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    provider = table.Column<string>(type: "text", nullable: false),
                    access_token = table.Column<string>(type: "text", nullable: true),
                    refresh_token = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_token", x => x.token_id);
                    table.ForeignKey(
                        name: "FK_token_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "email",
                columns: table => new
                {
                    email_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    from_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    to_address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    received_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    body = table.Column<string>(type: "text", nullable: true),
                    direction_id = table.Column<int>(type: "integer", nullable: false),
                    label_id = table.Column<int>(type: "integer", nullable: true),
                    history_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    snippet = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    plain_text = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("email_pkey", x => x.email_id);
                    table.ForeignKey(
                        name: "email_direction_id_fkey",
                        column: x => x.direction_id,
                        principalTable: "email_direction",
                        principalColumn: "direction_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "email_label_id_fkey",
                        column: x => x.label_id,
                        principalTable: "email_label",
                        principalColumn: "label_id");
                    table.ForeignKey(
                        name: "email_user_id_fkey",
                        column: x => x.user_id,
                        principalTable: "app_user",
                        principalColumn: "user_id");
                });

            migrationBuilder.InsertData(
                table: "email_direction",
                columns: new[] { "direction_id", "direction_name" },
                values: new object[,]
                {
                    { 1, "INBOX" },
                    { 2, "SENT" },
                    { 3, "DRAFT" }
                });

            migrationBuilder.CreateIndex(
                name: "email_direction_id_index",
                table: "email",
                column: "direction_id");

            migrationBuilder.CreateIndex(
                name: "email_label_id_index",
                table: "email",
                column: "label_id");

            migrationBuilder.CreateIndex(
                name: "IX_email_user_id",
                table: "email",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_token_user_id",
                table: "token",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email");

            migrationBuilder.DropTable(
                name: "token");

            migrationBuilder.DropTable(
                name: "email_direction");

            migrationBuilder.DropTable(
                name: "email_label");

            migrationBuilder.DropTable(
                name: "app_user");
        }
    }
}
