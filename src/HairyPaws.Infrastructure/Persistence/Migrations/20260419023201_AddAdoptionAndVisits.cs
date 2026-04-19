using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HairyPaws.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdoptionAndVisits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "adoption_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    pet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    adopter_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    contact_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    living_conditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    has_previous_pets = table.Column<bool>(type: "boolean", nullable: false),
                    why_adopt = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    review_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    reviewed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_adoption_requests", x => x.id);
                    table.ForeignKey(
                        name: "fk_adoption_requests_pets_pet_id",
                        column: x => x.pet_id,
                        principalTable: "pets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_adoption_requests_users_adopter_user_id",
                        column: x => x.adopter_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_adoption_requests_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "visits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    adoption_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_visits", x => x.id);
                    table.ForeignKey(
                        name: "fk_visits_adoption_requests_adoption_request_id",
                        column: x => x.adoption_request_id,
                        principalTable: "adoption_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_adoption_requests_adopter_user_id",
                table: "adoption_requests",
                column: "adopter_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_adoption_requests_created_at",
                table: "adoption_requests",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_adoption_requests_reviewed_by_user_id",
                table: "adoption_requests",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_adoption_requests_status",
                table: "adoption_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_adoption_requests_pet_id_adopter_user_id_active",
                table: "adoption_requests",
                columns: new[] { "pet_id", "adopter_user_id" },
                unique: true,
                filter: "\"status\" IN ('Submitted', 'UnderReview', 'Approved')");

            migrationBuilder.CreateIndex(
                name: "ux_adoption_requests_pet_id_single_approved",
                table: "adoption_requests",
                column: "pet_id",
                unique: true,
                filter: "\"status\" = 'Approved'");

            migrationBuilder.CreateIndex(
                name: "ix_visits_scheduled_at",
                table: "visits",
                column: "scheduled_at");

            migrationBuilder.CreateIndex(
                name: "ix_visits_status",
                table: "visits",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_visits_adoption_request_id_single_active",
                table: "visits",
                column: "adoption_request_id",
                unique: true,
                filter: "\"status\" IN ('Pending', 'Approved')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "visits");

            migrationBuilder.DropTable(
                name: "adoption_requests");
        }
    }
}
