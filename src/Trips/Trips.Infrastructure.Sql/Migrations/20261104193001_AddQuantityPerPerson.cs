using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trips.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddQuantityPerPerson : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Adults",
                schema: "trips",
                table: "Trips",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "Children",
                schema: "trips",
                table: "Trips",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "QuantityBasis",
                schema: "trips",
                table: "TripItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Total",
                schema: "trips",
                table: "TripItems",
                type: "int",
                nullable: true);

            // Every existing amount was a fixed number, so each item keeps exactly the total it showed before.
            migrationBuilder.Sql("UPDATE [trips].[TripItems] SET [QuantityBasis] = N'Fixed', [Total] = [Quantity] WHERE [Quantity] IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Adults",
                schema: "trips",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "Children",
                schema: "trips",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "QuantityBasis",
                schema: "trips",
                table: "TripItems");

            migrationBuilder.DropColumn(
                name: "Total",
                schema: "trips",
                table: "TripItems");
        }
    }
}
