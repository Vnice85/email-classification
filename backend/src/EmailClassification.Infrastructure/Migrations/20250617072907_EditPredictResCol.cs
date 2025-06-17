using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmailClassification.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EditPredictResCol : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PredictionResult",
                table: "email",
                newName: "prediction_result");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "prediction_result",
                table: "email",
                newName: "PredictionResult");
        }
    }
}
