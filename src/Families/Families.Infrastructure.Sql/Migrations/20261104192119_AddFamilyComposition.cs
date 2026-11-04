using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Families.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddFamilyComposition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Adults",
                schema: "families",
                table: "Families",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "Children",
                schema: "families",
                table: "Families",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Verburgh (KnownFamilies.Verburgh) has three children; the id is a literal because a migration must not change
            // when live code does.
            migrationBuilder.UpdateData(
                schema: "families",
                table: "Families",
                keyColumn: "Id",
                keyValue: new Guid("0196a3c2-5e10-7a41-9b2d-6f8e1c4d2b00"),
                column: "Children",
                value: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Adults",
                schema: "families",
                table: "Families");

            migrationBuilder.DropColumn(
                name: "Children",
                schema: "families",
                table: "Families");
        }
    }
}
