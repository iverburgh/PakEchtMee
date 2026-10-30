using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Families.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class InitialFamilies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "families");

            migrationBuilder.CreateTable(
                name: "Families",
                schema: "families",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false, collation: "Latin1_General_CI_AI")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Families", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Members",
                schema: "families",
                columns: table => new
                {
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Email);
                    table.ForeignKey(
                        name: "FK_Members_Families_FamilyId",
                        column: x => x.FamilyId,
                        principalSchema: "families",
                        principalTable: "Families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Families_Name",
                schema: "families",
                table: "Families",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_FamilyId",
                schema: "families",
                table: "Members",
                column: "FamilyId");

            // Everything that existed before families were introduced moves to Verburgh (KnownFamilies.Verburgh);
            // the id is a literal because a migration must not change when live code does.
            migrationBuilder.InsertData(
                schema: "families",
                table: "Families",
                columns: ["Id", "Name"],
                values: [new Guid("0196a3c2-5e10-7a41-9b2d-6f8e1c4d2b00"), "Verburgh"]);

            migrationBuilder.InsertData(
                schema: "families",
                table: "Members",
                columns: ["Email", "FamilyId"],
                values: ["ivoverburgh@gmail.com", new Guid("0196a3c2-5e10-7a41-9b2d-6f8e1c4d2b00")]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Members",
                schema: "families");

            migrationBuilder.DropTable(
                name: "Families",
                schema: "families");
        }
    }
}
