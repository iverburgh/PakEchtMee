using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Sql.Migrations
{
    /// <inheritdoc />
    public partial class AddQuantityBasis : Migration
    {
        /// <summary>Seeded general items that become 1 per adult; literals so this migration never changes with the seed.</summary>
        public static readonly Guid[] SeededAdultItems =
        [
            new("a6982e8c-ef28-569c-b31b-626e32d6fd49"), // Buitenkleding / Jas
            new("472493db-e230-538e-80ad-b9e6a1fed0ed"), // Buitenkleding / Slippers/crocs/teva's
            new("8693e7ac-d1e4-54c7-8332-9d3529b7d3ee"), // Buitenkleding / Pet
            new("9b4c5238-b901-5769-852e-7e0be42ecfad"), // Buitenkleding / Zonnebrillen
            new("9b207837-a7fb-5f2b-8ff4-8552f67e0b2d"), // Buitenkleding / Wandelschoenen
            new("9bd2bdbe-84be-5f47-9769-2337f5fdd73a"), // Toiletartikelen / Handdoeken douchen
            new("7b5ab920-14ea-5c0b-ada6-7525ea518a70"), // Slapen in tent / Matjes
            new("a37a09c4-b6a3-5a38-b663-751b4f4b33c9"), // Slapen in tent / Kussens
        ];

        /// <summary>Seeded general items that become 1 per child; literals so this migration never changes with the seed.</summary>
        public static readonly Guid[] SeededChildItems =
        [
            new("5f5e0a51-408e-52ac-8d51-d5df1aa4f172"), // Kinderen – Toiletartikelen / Handdoek(en) douchen
            new("3b7f112a-1901-5459-b002-7e941bb247c3"), // Kinderen – Buitenkleding / Jassen
            new("df8dddb2-8e64-58a4-ab2e-a3053af646de"), // Kinderen – Buitenkleding / Laarzen
            new("2e3d2476-6696-5e62-a420-97f7ddb5bd20"), // Kinderen – Buitenkleding / Schoenen
            new("4d700c38-cbcf-5dfa-9e71-81e5045590b1"), // Kinderen – Buitenkleding / Crocs/sandalen/slippers
            new("a254d248-f3f0-5148-81e5-bcb38b9c2b5f"), // Kinderen – Buitenkleding / Petten
            new("5642c93e-b279-5d33-bc3b-2391c55143be"), // Kinderen – Buitenkleding / Zonnebrillen
            new("3b9fc76e-baf0-5c9c-8954-05d2e6f6b2eb"), // Kinderen – Vermaak op locatie / Zwemkleding
            new("90e48163-b210-5c31-86a0-7e32527036e4"), // Kinderen – Vermaak op locatie / Handdoek(en) zwemmen
        ];

        /// <summary>General items that were seeded as quantity 3 and become 1 per child; literals so this migration never changes with the seed.</summary>
        public static readonly Guid[] SeededThreePerChildItems =
        [
            new("607828cc-0da5-52c6-a49c-49a1d8214e91"), // Kinderen – Slapen / Slaapzak
            new("81ff5b14-8208-5021-896c-0e6683515fd5"), // Kinderen – Slapen / Matje
            new("6e5b80c1-fa6c-56e0-a2fb-2a9684cca783"), // Kinderen – Slapen / Kussen
        ];

        /// <summary>Seeded general items that become 1 per person; literals so this migration never changes with the seed.</summary>
        public static readonly Guid[] SeededPersonItems =
        [
            new("19e91162-d3ef-5a7e-bb0a-c3c3318e721d"), // Vermaak op locatie / Badhanddoeken
            new("d3724350-8aed-5e0f-8328-5cbb2afceb6e"), // Algemeen / Geldige paspoorten
            new("afde61e0-fbae-5074-94d9-c831eb472c46"), // Kampeerkeuken / Borden
            new("9e0ee75e-5e39-5324-ab3b-69b43da087ca"), // Kampeerkeuken / Bekers
            new("4d2c36a0-7606-5ff2-b67d-6efa66b09e04"), // Kampeerkeuken / Bestek
            new("2af20909-0e5b-500f-9991-86d6edf143b9"), // Kamperen – Algemeen / Stoelen
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QuantityBasis",
                schema: "catalog",
                table: "Items",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // Every existing amount was a fixed number.
            migrationBuilder.Sql("UPDATE [catalog].[Items] SET [QuantityBasis] = N'Fixed' WHERE [Quantity] IS NOT NULL;");

            ConvertSeededItems(migrationBuilder, SeededAdultItems, "PerAdult", seededQuantity: null);
            ConvertSeededItems(migrationBuilder, SeededChildItems, "PerChild", seededQuantity: null);
            ConvertSeededItems(migrationBuilder, SeededThreePerChildItems, "PerChild", seededQuantity: 3);
            ConvertSeededItems(migrationBuilder, SeededPersonItems, "PerPerson", seededQuantity: null);
        }

        /// <summary>
        /// Gives the listed seeded items the rule 1 per head, but only while they are general and still carry exactly
        /// what the seed gave them: no unit, and no quantity or the seeded one. Anything the administrator changed is kept.
        /// </summary>
        private static void ConvertSeededItems(MigrationBuilder migrationBuilder, Guid[] ids, string basis, int? seededQuantity)
        {
            string idList = string.Join(", ", ids.Select(id => $"'{id}'"));
            string quantityCondition = seededQuantity is null ? "[i].[Quantity] IS NULL" : $"[i].[Quantity] = {seededQuantity}";

            migrationBuilder.Sql($"""
                UPDATE [i] SET [Quantity] = 1, [QuantityBasis] = N'{basis}'
                FROM [catalog].[Items] AS [i]
                JOIN [catalog].[Categories] AS [c] ON [c].[Id] = [i].[CategoryId]
                WHERE [i].[Id] IN ({idList}) AND [c].[FamilyId] IS NULL AND [i].[Unit] IS NULL AND {quantityCondition};
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuantityBasis",
                schema: "catalog",
                table: "Items");
        }
    }
}
