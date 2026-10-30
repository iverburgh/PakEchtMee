using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trips.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyToTrips : Migration
    {
        /// <summary>KnownFamilies.Verburgh at the time of writing; a literal because a migration must not follow live code.</summary>
        internal const string Verburgh = "0196a3c2-5e10-7a41-9b2d-6f8e1c4d2b00";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FamilyId",
                schema: "trips",
                table: "Trips",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid(Verburgh));

            // The default only exists to give every pre-existing trip to Verburgh; new trips always state their family.
            migrationBuilder.AlterColumn<Guid>(
                name: "FamilyId",
                schema: "trips",
                table: "Trips",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldDefaultValue: new Guid(Verburgh));

            migrationBuilder.CreateIndex(
                name: "IX_Trips_FamilyId",
                schema: "trips",
                table: "Trips",
                column: "FamilyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trips_FamilyId",
                schema: "trips",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "FamilyId",
                schema: "trips",
                table: "Trips");
        }
    }
}
