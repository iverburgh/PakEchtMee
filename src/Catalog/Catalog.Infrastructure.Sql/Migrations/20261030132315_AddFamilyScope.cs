using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyScope : Migration
    {
        /// <summary>The family every pre-existing own category moves to (KnownFamilies.Verburgh at the time of writing).</summary>
        internal const string Verburgh = "0196a3c2-5e10-7a41-9b2d-6f8e1c4d2b00";

        /// <summary>
        /// The 22 categories of the development seed at the time of writing (CatalogSeedData); they become the general
        /// catalog. Literals on purpose: a migration must not change when the seed definition does.
        /// </summary>
        internal static readonly string[] SeedCategoryIds =
        [
            "1cf89837-6765-542b-9b0f-694e854007d7",
            "6dcd8f33-0230-516c-9cc8-35862173189f",
            "6bb49279-4078-5de4-a163-49d946d28277",
            "88e04ff3-44ca-536e-9a58-dc9addff4c1a",
            "9613205d-5110-5cff-8dce-3f55d4a19c8e",
            "fd05ed76-5a57-50fa-a5f3-dd7de0268fe7",
            "e551826b-a326-53a6-a7f1-0f727da4c984",
            "e4194806-358f-55e5-8d09-2bbadb2271a9",
            "62bfa69f-7d93-5f46-b0cc-a045a93c12cf",
            "fd160137-e30f-584a-ad82-f9d40bee3643",
            "be446b6b-c043-5ab5-b452-3eb62377bafb",
            "0e53f923-0d9e-5c62-a586-cd1df0c946dd",
            "428940f1-49ed-5eaa-9032-19e1681b06c8",
            "20b729ac-a6e2-5a52-a0ba-a0723cf54d2a",
            "c847f001-fcc6-503e-bb2c-2d1b4552afaa",
            "a8538473-4878-5eb9-aa17-db53e1c1cf41",
            "619380fa-22e7-5b6b-8320-d2d448af39dd",
            "e1404b84-bdb0-5d3e-9e2c-9ae7d6b7a026",
            "53528a7b-1ce6-5344-8c84-661d2e56fcb5",
            "51f77ab7-8204-5a0d-89d6-000a12b720eb",
            "a555c4ae-d504-56c1-b620-154b38bfa76c",
            "09f2cb6f-ce7f-5700-af2e-063f8ee28f8a",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.AddColumn<Guid>(
                name: "FamilyId",
                schema: "catalog",
                table: "Categories",
                type: "uniqueidentifier",
                nullable: true);

            // Seeded categories stay general (FamilyId NULL); everything else was created by the Verburgh family.
            migrationBuilder.Sql($"""
                UPDATE [catalog].[Categories]
                SET [FamilyId] = '{Verburgh}'
                WHERE [Id] NOT IN ({string.Join(", ", SeedCategoryIds.Select(id => $"'{id}'"))})
                """);

            migrationBuilder.CreateTable(
                name: "FamilyCategoryOptOuts",
                schema: "catalog",
                columns: table => new
                {
                    FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyCategoryOptOuts", x => new { x.FamilyId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_FamilyCategoryOptOuts_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "catalog",
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_FamilyId_Name",
                schema: "catalog",
                table: "Categories",
                columns: new[] { "FamilyId", "Name" },
                unique: true,
                filter: "[DeletedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FamilyCategoryOptOuts_CategoryId",
                schema: "catalog",
                table: "FamilyCategoryOptOuts",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FamilyCategoryOptOuts",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "IX_Categories_FamilyId_Name",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                schema: "catalog",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                schema: "catalog",
                table: "Categories",
                column: "Name",
                unique: true,
                filter: "[DeletedAt] IS NULL");
        }
    }
}
