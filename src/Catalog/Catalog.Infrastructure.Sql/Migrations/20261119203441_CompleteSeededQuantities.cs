using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Catalog.Infrastructure.Sql.Migrations
{
    /// <summary>
    /// Gives every seeded general item that still has no amount its seeded rule, once per database. The rules are literal
    /// rows so this migration never changes with the seed; a test keeps them equal to <c>CatalogSeedData.SeedRules</c>.
    /// </summary>
    public partial class CompleteSeededQuantities : Migration
    {
        public sealed record Row(Guid Id, int Quantity, string Unit, string Basis, string Period);

        public static readonly IReadOnlyList<Row> Rows =
        [
            new(new Guid("34746f00-8008-5120-9c40-b50e966bbd9f"), 1, "pak", "PerChild", "PerTrip"), // Kinderen – Algemeen / Snoetenpoetsers
            new(new Guid("e1d3f73f-95db-5a74-8c6b-a03b74c7cfe8"), 1, "set", "PerChild", "PerTrip"), // Kinderen – Algemeen / Extra beddengoed
            new(new Guid("48c25d44-f3ea-5347-b7d8-444eee05f12d"), 1, null, "PerChild", "PerTrip"), // Kinderen – Algemeen / Dekens
            new(new Guid("b8914c9e-8434-5041-93f2-1c854ac08785"), 1, "flesje", "Fixed", "PerTrip"), // Kinderen – Toiletartikelen / Vitamine D
            new(new Guid("f680f0c9-7224-5526-a9cb-2aaf5e9e8f0d"), 1, null, "Fixed", "PerTrip"), // Kinderen – Toiletartikelen / Nagelknippertjes
            new(new Guid("faf8dc87-3e73-5762-b7ca-c903fa86a81a"), 1, null, "PerChild", "PerTrip"), // Kinderen – Toiletartikelen / Tandenborstel(s) + tandpasta
            new(new Guid("17fd6e77-3fdf-52aa-845a-0aea081b45d3"), 1, "set", "Fixed", "PerTrip"), // Kinderen – Toiletartikelen / Zalf en cremes
            new(new Guid("92a2afd8-86ce-59f2-b581-c86f1a8f04d2"), 1, null, "PerChild", "PerTrip"), // Kinderen – Slapen / Knuffels
            new(new Guid("7d484a4c-10db-54f0-9731-ef3b4d271c12"), 2, null, "PerChild", "PerTrip"), // Kinderen – Slapen / Pyjama's
            new(new Guid("5899e410-3883-58f7-854f-67a0b0c94a87"), 1, null, "PerChild", "PerTrip"), // Kinderen – Slapen / Badjas
            new(new Guid("3005da86-475c-5596-837a-cb76bcc90a25"), 1, null, "Fixed", "PerTrip"), // Kinderen – Slapen / Wekker
            new(new Guid("5c243983-9c9b-5e57-8cef-61acbf5377cd"), 1, null, "PerChild", "PerTrip"), // Kinderen – Buitenkleding / Paraplu
            new(new Guid("7d95a28d-5a4c-52fd-b43b-d29dd3a01a5d"), 4, null, "PerChild", "PerTrip"), // Kinderen – Kleding / Lange broeken/ korte broeken
            new(new Guid("3db2b684-3eb2-5115-b35b-1d90788fc57a"), 1, null, "PerChild", "PerDay"), // Kinderen – Kleding / Shirts korte & lange mouwen
            new(new Guid("c5b581b0-a9bf-5d7a-a463-2d51c3e95fee"), 2, null, "PerChild", "PerTrip"), // Kinderen – Kleding / Truien
            new(new Guid("e0defb6a-4b91-5f84-b512-8dbdf36a7102"), 3, null, "Fixed", "PerTrip"), // Kinderen – Kleding / Jurken
            new(new Guid("a03691a9-d6cd-5e51-93ac-ad9236e70bd6"), 1, "tas", "Fixed", "PerTrip"), // Kinderen – Vermaak op locatie / Speelgoed
            new(new Guid("9c844e9e-901c-559e-8e69-4898cc63e731"), 1, "set", "Fixed", "PerTrip"), // Kinderen – Vermaak op locatie / Duikringen
            new(new Guid("05e3795e-a565-525b-bd40-6a6a057ce8b5"), 2, null, "PerChild", "PerTrip"), // Kinderen – Vermaak op locatie / Boekjes
            new(new Guid("a10a022a-2ad5-5907-8ac0-742f6764ef22"), 1, null, "Fixed", "PerTrip"), // Kinderen – Vermaak op locatie / Opblaasband
            new(new Guid("be57d5cf-c448-52b5-a993-0e4187689911"), 1, "paar", "PerChild", "PerTrip"), // Kinderen – Vermaak op locatie / Zwembandjes
            new(new Guid("6d13817b-deee-5cd1-90c5-1515066eaaf9"), 1, null, "Fixed", "PerTrip"), // Kinderen – Vermaak op locatie / Drijfpakje
            new(new Guid("d17abbad-1026-559b-b587-a73dcdca6a6e"), 1, "set", "Fixed", "PerTrip"), // Kinderen – Vermaak op locatie / Potloden + puntenslijper
            new(new Guid("ea3395de-ed64-5b22-83ce-ecfbf7c3c8fe"), 1, null, "PerChild", "PerTrip"), // Kinderen – Vermaak op locatie / Speelboeken
            new(new Guid("fb54ad8c-fd7f-585f-a7ff-75b4e16dfbd1"), 1, "set", "Fixed", "PerTrip"), // Kinderen – Vermaak op locatie / Tafeltennisspullen
            new(new Guid("4ec52e1c-7848-55c1-9c68-1fc13ba88095"), 1, "set", "Fixed", "PerTrip"), // Kinderen – Vermaak op locatie / Badminton rackets
            new(new Guid("8fc6b24a-89c5-521e-b38d-6bb8a8e45630"), 1, "set", "Fixed", "PerTrip"), // Kinderen – Vermaak op locatie / Klitterband bal
            new(new Guid("2f062f6c-4e3c-5a9d-8753-5a411834904d"), 1, null, "PerChild", "PerTrip"), // Kinderen – Vermaak op locatie / Zwembrillen
            new(new Guid("42cb2e19-23e3-5382-ba59-095b2adf6110"), 1, null, "PerChild", "PerTrip"), // Kinderen – Vermaak op locatie / Zeemeerminstaarten
            new(new Guid("a814df67-6de9-58de-971a-5ac32199d2ad"), 1, null, "PerChild", "PerTrip"), // Kinderen – Vermaak op locatie / Waterspuiten
            new(new Guid("ee890216-240d-5565-9a9b-cd052b0cea2e"), 1, null, "PerAdult", "PerDay"), // Kleding / T-shirts
            new(new Guid("2d96f2f6-ca27-55a8-922e-e341943e56d3"), 3, null, "PerAdult", "PerTrip"), // Kleding / Korte + lange broeken
            new(new Guid("19fc534a-374b-5d1c-a693-8cfdba2cddab"), 1, null, "PerAdult", "PerTrip"), // Kleding / Vest
            new(new Guid("3cc5a6f9-247d-5577-84a4-468ca3e26580"), 3, null, "Fixed", "PerTrip"), // Kleding Martine / BH's
            new(new Guid("361befad-8c61-57f8-8ab6-98b5f93743ef"), 2, null, "Fixed", "PerTrip"), // Kleding Martine / Jurk + ondergoed
            new(new Guid("75b6db8f-6231-5de1-8eca-0eb6c5855976"), 1, null, "Fixed", "PerTrip"), // Kleding Martine / Badjas
            new(new Guid("9b93b4e6-7b1d-5816-92b3-525c386cfa34"), 1, null, "PerAdult", "PerTrip"), // Buitenkleding / Paraplu
            new(new Guid("082af120-07e1-54c9-aa34-8c5773235433"), 1, "potje", "Fixed", "PerTrip"), // Toiletartikelen / Vitaminepillen
            new(new Guid("7d1bcfa1-2d1c-50d6-9d75-ff244a7d7ff6"), 1, null, "Fixed", "PerTrip"), // Toiletartikelen / Scheermesje
            new(new Guid("94eb2fa4-0d20-5dc2-8d71-b4ab9f90fcc8"), 1, "doosje", "Fixed", "PerTrip"), // Toiletartikelen / Oordoppen Martine
            new(new Guid("c6ccc23d-9a40-591e-85e7-85a6e6934f8d"), 1, "fles", "Fixed", "PerTrip"), // Toiletartikelen / Shampoo
            new(new Guid("128767b3-0952-57a8-8fbf-afe46f83a0b3"), 1, null, "PerAdult", "PerTrip"), // Toiletartikelen / Tandenborstel + pasta
            new(new Guid("7b6c093e-3224-5e4c-bacb-bcedca1cad6a"), 1, "fles", "Fixed", "PerTrip"), // Toiletartikelen / Listerine
            new(new Guid("c0041b53-ff88-5bb5-a5b4-7976b6f443bb"), 1, null, "Fixed", "PerTrip"), // Toiletartikelen / Nagelknippers
            new(new Guid("e77e7134-a23a-594c-87b5-064e42257948"), 1, null, "Fixed", "PerTrip"), // Toiletartikelen / Tekentang
            new(new Guid("bc853ee1-e24a-5cb9-8eaf-0f84a11583bf"), 1, "fles", "Fixed", "PerTrip"), // Toiletartikelen / Antimuggenspul
            new(new Guid("0b642fb2-ef02-54b3-9cc6-a59464c27eea"), 1, null, "PerAdult", "PerTrip"), // Toiletartikelen / Blistex
            new(new Guid("b9cfe327-6847-5800-b297-dcc4273eeec2"), 1, "fles", "Fixed", "PerTrip"), // Toiletartikelen Martine / Conditioner
            new(new Guid("81b97242-5ab7-57d4-84c2-5e87f12d423b"), 1, null, "Fixed", "PerTrip"), // Toiletartikelen Martine / Borstel
            new(new Guid("3ad5551d-cfd6-5724-af83-401c413e6b7e"), 1, "doos", "Fixed", "PerTrip"), // Toiletartikelen Martine / Tampons
            new(new Guid("e3e627a9-04db-55b3-9003-bd19c7c59333"), 1, "set", "Fixed", "PerTrip"), // Toiletartikelen Martine / Scrub / tea tree / sudocrem
            new(new Guid("acf1834c-48af-5496-9c80-e13f51146be3"), 1, null, "PerAdult", "PerTrip"), // Vermaak op locatie / Zwemkleding I & M
            new(new Guid("938fa080-be4b-5a78-ae69-ab4a995dbc11"), 1, "set", "Fixed", "PerTrip"), // Vermaak op locatie / Sportspullen
            new(new Guid("088606ca-81db-5b4b-88ef-a3bf9e84fa2b"), 1, null, "PerAdult", "PerTrip"), // Vermaak op locatie / Boeken / E-reader
            new(new Guid("54e3eece-d326-532e-9db0-c8988fdf2a32"), 1, null, "Fixed", "PerTrip"), // Vermaak op locatie / Schaakspel
            new(new Guid("4936690f-ae75-5638-9806-ab140444f5cc"), 2, null, "Fixed", "PerTrip"), // Vermaak op locatie / Spelletjes
            new(new Guid("c06f5072-592f-524c-8a29-fe974033ebc9"), 1, null, "PerPerson", "PerTrip"), // Vervoer zonder auto / Rugzakken
            new(new Guid("9e81066d-5264-5f09-ab94-0c9f76f16328"), 1, "set", "Fixed", "PerTrip"), // Algemeen / Pen & notitieblokje
            new(new Guid("4d8772b5-2900-56fa-880b-57d87809f800"), 1, null, "Fixed", "PerTrip"), // Algemeen / Geldige creditcard
            new(new Guid("c6406354-ef39-5086-a9c7-b4dfd3c64cfc"), 1, null, "PerAdult", "PerTrip"), // Algemeen / Portemonnees
            new(new Guid("b5cb8c11-8685-5ff3-80e3-af39ac6b3775"), 2, null, "Fixed", "PerTrip"), // Algemeen / Opgeladen powerbank
            new(new Guid("db397bb6-d8ae-5076-a2ed-3221d2efc63c"), 1, null, "PerAdult", "PerTrip"), // Algemeen / Telefoonladers
            new(new Guid("ff0614f2-c7d3-516e-ab54-85861865d8cd"), 1, null, "Fixed", "PerTrip"), // Algemeen / Horlogeladers
            new(new Guid("5ba303fb-4244-5dd0-910f-375002cb550e"), 1, null, "Fixed", "PerTrip"), // Algemeen / Huissleutel
            new(new Guid("343ca1b0-7a9c-5993-a0a4-55a3a22e60b3"), 1, null, "Fixed", "PerTrip"), // Algemeen / Reservesleutel auto
            new(new Guid("743d8bc2-1dde-56e5-809d-9237e4f69f38"), 1, null, "Fixed", "PerTrip"), // Algemeen / EHBO-set
            new(new Guid("12d14091-cbe6-5e8a-ba42-56276699a3af"), 2, "fles", "Fixed", "PerTrip"), // Algemeen / Zonnebrand
            new(new Guid("5d30341b-9d29-5fd5-89c2-e9e8c6664dbb"), 1, "pak", "PerPerson", "PerTrip"), // Algemeen / Zakdoekjes
            new(new Guid("b6670c12-d077-5cdf-9c96-b56b599de7af"), 3, null, "Fixed", "PerTrip"), // Algemeen / Shoppers
            new(new Guid("18fd894d-afde-522c-ab32-2c2512c4da71"), 2, null, "Fixed", "PerTrip"), // Algemeen / Boodschappenkratten
            new(new Guid("6bf4cc9b-3542-5786-a8f2-6dc0bb703506"), 1, null, "Fixed", "PerTrip"), // Algemeen / Vuile waszak
            new(new Guid("acbe4c6a-ee09-583b-abe2-0091eb754fb6"), 4, null, "Fixed", "PerTrip"), // Algemeen / Kleine stuffbags
            new(new Guid("08f1fd0f-0e84-5f10-9597-4b71b8799e5f"), 1, null, "PerPerson", "PerTrip"), // Algemeen / Waterflesjes / Dopper
            new(new Guid("fadf29e8-1b61-54f1-9f01-7094ea08b3cd"), 1, null, "Fixed", "PerTrip"), // Algemeen / Cash
            new(new Guid("13e3b732-fa61-5857-b901-12ace007c118"), 1, null, "PerAdult", "PerTrip"), // Algemeen / Oordopjes voor muziek
            new(new Guid("49c99e25-18f4-562c-bfe9-3f59e9a5ef77"), 1, "fles", "Fixed", "PerTrip"), // Keukengerei / Afwasmiddel
            new(new Guid("6a0a16a1-b804-5b4f-9f33-305b505adee4"), 3, null, "Fixed", "PerTrip"), // Keukengerei / Theedoeken
            new(new Guid("180aefbb-2fd9-59c1-94bf-d7e6f33c0b11"), 3, null, "Fixed", "PerTrip"), // Keukengerei / Doekjes
            new(new Guid("422f751d-8faa-5cae-b806-a221b199d9ec"), 4, null, "Fixed", "PerTrip"), // Keukengerei / Tupperware bakjes
            new(new Guid("8c9909db-7429-5a28-bc28-e8d482691d62"), 1, "set", "Fixed", "PerTrip"), // Koken / Kruiden
            new(new Guid("7c52fecd-7d15-56ba-aeb7-95b606e84a39"), 1, "fles", "Fixed", "PerTrip"), // Koken / Olijfolie
            new(new Guid("352e1b44-dcfc-523e-91c0-f16d3fd5544a"), 1, null, "Fixed", "PerTrip"), // Tent / Tent + pomp
            new(new Guid("11825225-47e1-5332-a8b3-ca311b0ea592"), 30, null, "Fixed", "PerTrip"), // Tent / Haringen
            new(new Guid("62ab5781-8832-5aae-aa73-8733d79a8961"), 1, null, "Fixed", "PerTrip"), // Tent / Tarp
            new(new Guid("6f124390-2e86-5d82-9277-bded6014c9c8"), 1, null, "Fixed", "PerTrip"), // Tent / Tenthamer
            new(new Guid("af706201-2a7b-5160-b9e6-97aa07c21155"), 1, "set", "Fixed", "PerTrip"), // Tent / Veger en blik
            new(new Guid("874a99be-8344-53b6-af6f-51dec83caab0"), 1, null, "Fixed", "PerTrip"), // Tent / Inloop/modderdoek
            new(new Guid("94557aa0-1b03-5808-94e4-37ef30a33f85"), 1, "set", "PerAdult", "PerTrip"), // Slapen in tent / Beddengoed
            new(new Guid("423e2acd-be13-5004-a5c3-a07cc560913b"), 1, null, "PerAdult", "PerTrip"), // Slapen in tent / Matrashoes
            new(new Guid("228936b8-a72e-503f-a88e-9c5e552d2395"), 1, null, "Fixed", "PerTrip"), // Kampeerkeuken / Keukenkastje
            new(new Guid("b44e4330-b0e7-5f28-aff0-c675c0524d6c"), 1, "set", "Fixed", "PerTrip"), // Kampeerkeuken / Koelbox + elementen
            new(new Guid("6ae490ea-118f-53db-aeda-921b230e323d"), 1, "rol", "Fixed", "PerTrip"), // Kampeerkeuken / Vuilniszakken
            new(new Guid("653417d5-c30b-5853-b9e9-a43b636a4e8d"), 1, null, "Fixed", "PerTrip"), // Kampeerkeuken / Brander
            new(new Guid("515170e2-33ad-56c4-a9dc-c1a1ba9d7c4d"), 2, "bus", "Fixed", "PerTrip"), // Kampeerkeuken / Brandstof
            new(new Guid("eac8846e-da83-57f4-b13b-67cba07965d8"), 1, "set", "Fixed", "PerTrip"), // Kampeerkeuken / Lucifers + aansteker
            new(new Guid("d2c1a39f-231f-5e6f-865d-17730ce520f3"), 3, null, "Fixed", "PerTrip"), // Kampeerkeuken / Pannen
            new(new Guid("6ba7c71c-0276-59af-b6ba-afcb47450d31"), 1, null, "Fixed", "PerTrip"), // Kampeerkeuken / Spatel
            new(new Guid("56a8e0a8-3175-550e-a4d5-07f2f8a15bab"), 1, null, "Fixed", "PerTrip"), // Kampeerkeuken / Schaar
            new(new Guid("696bc2bd-1e45-5c26-9163-1c13c2b9aff5"), 1, null, "Fixed", "PerTrip"), // Kampeerkeuken / Handmixer
            new(new Guid("a2e22ffa-c8e0-5799-a4e8-872d17cfd1b2"), 1, null, "Fixed", "PerTrip"), // Kampeerkeuken / Broodmes
            new(new Guid("e90eedc6-2c76-5438-b365-592fdbc3fa9d"), 1, null, "Fixed", "PerTrip"), // Kampeerkeuken / Maatbeker
            new(new Guid("f6e28fc8-7d75-5a54-b188-a69adb0e9846"), 1, null, "Fixed", "PerTrip"), // Kampeerkeuken / Opinel zakmes
            new(new Guid("8fdf7b5d-3103-56a7-ac32-9584267c1f3e"), 1, null, "Fixed", "PerTrip"), // Kampeerkeuken / Snijplank
            new(new Guid("3682b59f-c2d6-5675-a7f5-6fbe67f09dfa"), 1, null, "Fixed", "PerTrip"), // Kampeerkeuken / Keukenmes
            new(new Guid("5b77ecd3-5576-54f0-b49c-19c774095c44"), 1, null, "Fixed", "PerTrip"), // Kampeerkeuken / Opvouwvergiet
            new(new Guid("86a313db-16bb-53ed-ba4e-d146cce905d9"), 1, "set", "Fixed", "PerTrip"), // Afwas / Sponsje + borstel
            new(new Guid("32bc343e-a120-5c0d-8423-20813020feb2"), 1, null, "Fixed", "PerTrip"), // Afwas / Emmer
            new(new Guid("44358f81-2a1e-5901-80f0-389843fce1d7"), 1, "fles", "Fixed", "PerTrip"), // Afwas / Afwasmiddel
            new(new Guid("abb073f6-f7eb-5fea-bdaf-e3f29eb78167"), 1, null, "Fixed", "PerTrip"), // Kamperen – Algemeen / Tafel
            new(new Guid("7cb0318f-9484-5695-8223-0c02ae191c8c"), 1, "rol", "Fixed", "PerDay"), // Kamperen – Algemeen / WC-papier
            new(new Guid("57b7f03a-d7c0-5504-b939-aa6b9d14e171"), 1, "rol", "Fixed", "PerTrip"), // Kamperen – Algemeen / Ducttape
            new(new Guid("0d51e267-84a6-5675-8368-23f82a406a57"), 1, null, "Fixed", "PerTrip"), // Kamperen – Algemeen / Zwitsers zakmes
            new(new Guid("5898924f-f846-5666-bfef-b83e9f461d7d"), 1, null, "Fixed", "PerTrip"), // Kamperen – Algemeen / Leatherman zakmes
            new(new Guid("2d646434-dbba-5a50-8612-4492ee14bab9"), 1, null, "PerPerson", "PerTrip"), // Kamperen – Algemeen / Lampen
            new(new Guid("7ad087aa-e6cb-54f5-b67a-1985074c347f"), 1, null, "Fixed", "PerTrip"), // Kamperen – Algemeen / Plasemmer
            new(new Guid("ba9bc12b-9326-5581-b729-ddf30f224e8f"), 1, null, "Fixed", "PerTrip"), // Kamperen – Algemeen / Hangmat + standaard
            new(new Guid("f03c1d9f-70de-5887-a9ad-7b461b8640ca"), 1, null, "Fixed", "PerTrip"), // Was / Waslijn
            new(new Guid("0411d882-923b-584f-aa05-0603e6e7fb80"), 1, "rol", "Fixed", "PerTrip"), // Was / Touw
            new(new Guid("b83ca0d4-07c9-550b-91ff-e8ee4da87c7c"), 20, null, "Fixed", "PerTrip"), // Was / Knijpers
            new(new Guid("2451c454-0182-5421-8a22-f646087c5de2"), 1, "fles", "Fixed", "PerTrip"), // Was / Wasmiddel
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            string values = string.Join(
                ",\n    ",
                Rows.Select(row => string.Create(
                    CultureInfo.InvariantCulture,
                    $"('{row.Id}', {row.Quantity}, {(row.Unit is null ? "NULL" : $"N'{row.Unit}'")}, N'{row.Basis}', N'{row.Period}')")));

            // Only items that are general and still exactly as seeded (no quantity, no unit) are filled in; anything an
            // administrator already gave an amount is kept.
            migrationBuilder.Sql($"""
                UPDATE [i]
                SET [Quantity] = [v].[Quantity], [Unit] = [v].[Unit], [QuantityBasis] = [v].[Basis], [QuantityPeriod] = [v].[Period]
                FROM [catalog].[Items] AS [i]
                JOIN [catalog].[Categories] AS [c] ON [c].[Id] = [i].[CategoryId]
                JOIN (VALUES
                    {values}
                ) AS [v] ([Id], [Quantity], [Unit], [Basis], [Period]) ON [v].[Id] = [i].[Id]
                WHERE [c].[FamilyId] IS NULL AND [i].[Quantity] IS NULL AND [i].[Unit] IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Filled-in amounts are left in place: they cannot be told apart from amounts set by hand afterwards.
        }
    }
}
