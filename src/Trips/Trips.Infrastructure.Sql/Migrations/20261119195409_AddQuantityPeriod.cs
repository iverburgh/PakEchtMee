using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Trips.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddQuantityPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QuantityPeriod",
                schema: "trips",
                table: "TripItems",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // Every existing rule was needed once per trip, so every stored total stays exactly what it was.
            migrationBuilder.Sql("UPDATE [trips].[TripItems] SET [QuantityPeriod] = N'PerTrip' WHERE [Quantity] IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuantityPeriod",
                schema: "trips",
                table: "TripItems");
        }
    }
}
