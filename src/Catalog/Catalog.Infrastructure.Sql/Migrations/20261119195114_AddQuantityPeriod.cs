using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddQuantityPeriod : Migration
    {
        /// <summary>Seeded general items that become 1 per adult per day; literals so this migration never changes with the seed.</summary>
        public static readonly Guid[] SeededAdultPerDayItems =
        [
            new("a036cbac-fac9-51ec-ba0c-467e5f283208"), // Kleding / Onderbroeken
            new("1f4fd6c5-81d8-57eb-be49-73c33f304831"), // Kleding / Sokken
        ];

        /// <summary>Seeded general items that become 1 per child per day; literals so this migration never changes with the seed.</summary>
        public static readonly Guid[] SeededChildPerDayItems =
        [
            new("c5923a22-8772-592f-9eee-7eb49422d668"), // Kinderen – Kleding / Sokken
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QuantityPeriod",
                schema: "catalog",
                table: "Items",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // Every existing amount was needed once per trip.
            migrationBuilder.Sql("UPDATE [catalog].[Items] SET [QuantityPeriod] = N'PerTrip' WHERE [Quantity] IS NOT NULL;");

            ConvertSeededItems(migrationBuilder, SeededAdultPerDayItems, "PerAdult");
            ConvertSeededItems(migrationBuilder, SeededChildPerDayItems, "PerChild");
        }

        /// <summary>
        /// Gives the listed seeded clothing the rule 1 per head per day, but only while it is general and still has no
        /// amount at all, as seeded. Anything the administrator gave an amount is kept.
        /// </summary>
        private static void ConvertSeededItems(MigrationBuilder migrationBuilder, Guid[] ids, string basis)
        {
            string idList = string.Join(", ", ids.Select(id => $"'{id}'"));

            migrationBuilder.Sql($"""
                UPDATE [i] SET [Quantity] = 1, [QuantityBasis] = N'{basis}', [QuantityPeriod] = N'PerDay'
                FROM [catalog].[Items] AS [i]
                JOIN [catalog].[Categories] AS [c] ON [c].[Id] = [i].[CategoryId]
                WHERE [i].[Id] IN ({idList}) AND [c].[FamilyId] IS NULL AND [i].[Quantity] IS NULL AND [i].[Unit] IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuantityPeriod",
                schema: "catalog",
                table: "Items");
        }
    }
}
