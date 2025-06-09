using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmailClassification.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EditAppUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "app_user",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_temp",
                table: "app_user",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_at",
                table: "app_user");

            migrationBuilder.DropColumn(
                name: "is_temp",
                table: "app_user");
        }
    }
}
