using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HairyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdoptionAndVisitLookupIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_visits_adoption_request_id",
                table: "visits",
                column: "adoption_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_adoption_requests_pet_id",
                table: "adoption_requests",
                column: "pet_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_visits_adoption_request_id",
                table: "visits");

            migrationBuilder.DropIndex(
                name: "ix_adoption_requests_pet_id",
                table: "adoption_requests");
        }
    }
}
