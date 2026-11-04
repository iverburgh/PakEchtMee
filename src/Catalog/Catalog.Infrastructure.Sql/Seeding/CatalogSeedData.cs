using Catalog.Domain.Items;

namespace Catalog.Infrastructure.Sql.Seeding;

public sealed record SeedItem(Guid Id, string Name, int? Quantity, QuantityBasis Basis = QuantityBasis.Fixed);

public sealed record SeedCategory(Guid Id, string Name, IReadOnlyList<SeedItem> Items);

/// <summary>
/// The family packing list ("Paklijst vakanties en weekendjes weg 2026"), converted by hand from the Excel sheet.
/// Names are the ids' source, so changing a name here seeds a new record instead of renaming the old one.
/// </summary>
public static class CatalogSeedData
{
    public const string KinderenAlgemeen = "Kinderen – Algemeen";
    public const string KinderenToiletartikelen = "Kinderen – Toiletartikelen";
    public const string KinderenSlapen = "Kinderen – Slapen";
    public const string KinderenBuitenkleding = "Kinderen – Buitenkleding";
    public const string KinderenKleding = "Kinderen – Kleding";
    public const string KinderenVermaak = "Kinderen – Vermaak op locatie";
    public const string Kleding = "Kleding";
    public const string KledingMartine = "Kleding Martine";
    public const string Buitenkleding = "Buitenkleding";
    public const string Toiletartikelen = "Toiletartikelen";
    public const string ToiletartikelenMartine = "Toiletartikelen Martine";
    public const string Vermaak = "Vermaak op locatie";
    public const string VervoerZonderAuto = "Vervoer zonder auto";
    public const string Algemeen = "Algemeen";
    public const string Keukengerei = "Keukengerei";
    public const string Koken = "Koken";
    public const string Tent = "Tent";
    public const string SlapenInTent = "Slapen in tent";
    public const string Kampeerkeuken = "Kampeerkeuken";
    public const string Afwas = "Afwas";
    public const string KamperenAlgemeen = "Kamperen – Algemeen";
    public const string Was = "Was";

    /// <summary>The categories that only make sense when camping.</summary>
    public static readonly IReadOnlyList<string> CampingCategories =
        [Tent, SlapenInTent, Kampeerkeuken, Afwas, KamperenAlgemeen, Was];

    /// <summary>
    /// Items packed once for every child, adult or person on a trip. The sheet's "3x" entries were three children, so
    /// they are per child here. Declared before <see cref="Categories"/>, which reads it while it is initialised.
    /// </summary>
    public static readonly IReadOnlyDictionary<(string Category, string Item), QuantityBasis> PerHead =
        new Dictionary<(string Category, string Item), QuantityBasis>
        {
            [(KinderenSlapen, "Slaapzak")] = QuantityBasis.PerChild,
            [(KinderenSlapen, "Matje")] = QuantityBasis.PerChild,
            [(KinderenSlapen, "Kussen")] = QuantityBasis.PerChild,
            [(KinderenToiletartikelen, "Handdoek(en) douchen")] = QuantityBasis.PerChild,
            [(KinderenBuitenkleding, "Jassen")] = QuantityBasis.PerChild,
            [(KinderenBuitenkleding, "Laarzen")] = QuantityBasis.PerChild,
            [(KinderenBuitenkleding, "Schoenen")] = QuantityBasis.PerChild,
            [(KinderenBuitenkleding, "Crocs/sandalen/slippers")] = QuantityBasis.PerChild,
            [(KinderenBuitenkleding, "Petten")] = QuantityBasis.PerChild,
            [(KinderenBuitenkleding, "Zonnebrillen")] = QuantityBasis.PerChild,
            [(KinderenVermaak, "Zwemkleding")] = QuantityBasis.PerChild,
            [(KinderenVermaak, "Handdoek(en) zwemmen")] = QuantityBasis.PerChild,
            [(Buitenkleding, "Jas")] = QuantityBasis.PerAdult,
            [(Buitenkleding, "Slippers/crocs/teva's")] = QuantityBasis.PerAdult,
            [(Buitenkleding, "Pet")] = QuantityBasis.PerAdult,
            [(Buitenkleding, "Zonnebrillen")] = QuantityBasis.PerAdult,
            [(Buitenkleding, "Wandelschoenen")] = QuantityBasis.PerAdult,
            [(Toiletartikelen, "Handdoeken douchen")] = QuantityBasis.PerAdult,
            [(SlapenInTent, "Matjes")] = QuantityBasis.PerAdult,
            [(SlapenInTent, "Kussens")] = QuantityBasis.PerAdult,
            [(Vermaak, "Badhanddoeken")] = QuantityBasis.PerPerson,
            [(Algemeen, "Geldige paspoorten")] = QuantityBasis.PerPerson,
            [(Kampeerkeuken, "Borden")] = QuantityBasis.PerPerson,
            [(Kampeerkeuken, "Bekers")] = QuantityBasis.PerPerson,
            [(Kampeerkeuken, "Bestek")] = QuantityBasis.PerPerson,
            [(KamperenAlgemeen, "Stoelen")] = QuantityBasis.PerPerson,
        };

    public static readonly IReadOnlyList<SeedCategory> Categories =
    [
        Category(KinderenAlgemeen, "Snoetenpoetsers", "Extra beddengoed", "Dekens"),
        Category(KinderenToiletartikelen, "Vitamine D", "Handdoek(en) douchen", "Nagelknippertjes", "Tandenborstel(s) + tandpasta", "Zalf en cremes"),
        Category(KinderenSlapen, "Knuffels", "Pyjama's", "Badjas", "Wekker", "Slaapzak", "Matje", "Kussen"),
        Category(KinderenBuitenkleding, "Jassen", "Laarzen", "Schoenen", "Paraplu", "Crocs/sandalen/slippers", "Petten", "Zonnebrillen"),
        Category(KinderenKleding, "Lange broeken/ korte broeken", "Shirts korte & lange mouwen", "Sokken", "Truien", "Jurken"),
        Category(
            KinderenVermaak,
            "Speelgoed", "Duikringen", "Boekjes", "Opblaasband", "Handdoek(en) zwemmen", "Zwemkleding", "Zwembandjes", "Drijfpakje",
            "Potloden + puntenslijper", "Speelboeken", "Tafeltennisspullen", "Badminton rackets", "Klitterband bal", "Zwembrillen",
            "Zeemeerminstaarten", "Waterspuiten"),
        Category(Kleding, "Onderbroeken", "Sokken", "T-shirts", "Korte + lange broeken", "Vest"),
        Category(KledingMartine, "BH's", "Jurk + ondergoed", "Badjas"),
        Category(Buitenkleding, "Jas", "Slippers/crocs/teva's", "Pet", "Paraplu", "Zonnebrillen", "Wandelschoenen"),
        Category(
            Toiletartikelen,
            "Vitaminepillen", "Scheermesje", "Oordoppen Martine", "Shampoo", "Tandenborstel + pasta", "Listerine", "Handdoeken douchen",
            "Nagelknippers", "Tekentang", "Antimuggenspul", "Blistex"),
        Category(ToiletartikelenMartine, "Conditioner", "Borstel", "Tampons", "Scrub / tea tree / sudocrem"),
        Category(Vermaak, "Zwemkleding I & M", "Badhanddoeken", "Sportspullen", "Boeken / E-reader", "Schaakspel", "Spelletjes"),
        Category(VervoerZonderAuto, "Rugzakken"),
        Category(
            Algemeen,
            "Pen & notitieblokje", "Geldige paspoorten", "Geldige creditcard", "Portemonnees", "Opgeladen powerbank", "Telefoonladers",
            "Horlogeladers", "Huissleutel", "Reservesleutel auto", "EHBO-set", "Zonnebrand", "Zakdoekjes", "Shoppers", "Boodschappenkratten",
            "Vuile waszak", "Kleine stuffbags", "Waterflesjes / Dopper", "Cash", "Oordopjes voor muziek"),
        Category(Keukengerei, "Afwasmiddel", "Theedoeken", "Doekjes", "Tupperware bakjes"),
        Category(Koken, "Kruiden", "Olijfolie"),
        Category(Tent, "Tent + pomp", "Haringen", "Tarp", "Tenthamer", "Veger en blik", "Inloop/modderdoek"),
        Category(SlapenInTent, "Matjes", "Beddengoed", "Kussens", "Matrashoes"),
        Category(
            Kampeerkeuken,
            "Keukenkastje", "Koelbox + elementen", "Vuilniszakken", "Brander", "Brandstof", "Lucifers + aansteker", "Pannen", "Spatel",
            "Bestek", "Borden", "Bekers", "Schaar", "Handmixer", "Broodmes", "Maatbeker", "Opinel zakmes", "Snijplank", "Keukenmes",
            "Opvouwvergiet"),
        Category(Afwas, "Sponsje + borstel", "Emmer", "Afwasmiddel"),
        Category(
            KamperenAlgemeen,
            "Tafel", "Stoelen", "WC-papier", "Ducttape", "Zwitsers zakmes", "Leatherman zakmes", "Lampen", "Plasemmer", "Hangmat + standaard"),
        Category(Was, "Waslijn", "Touw", "Knijpers", "Wasmiddel"),
    ];

    public static Guid CategoryId(string categoryName) => SeedId.For($"category:{categoryName}");

    public static Guid ItemId(string categoryName, string itemName) => SeedId.For($"item:{categoryName}/{itemName}");

    private static SeedCategory Category(string name, params string[] itemNames)
        => Category(name, [.. itemNames.Select(itemName => Item(name, itemName))]);

    private static SeedCategory Category(string name, IReadOnlyList<SeedItem> items) => new(CategoryId(name), name, items);

    private static SeedItem Item(string categoryName, string name)
        => PerHead.TryGetValue((categoryName, name), out QuantityBasis basis)
            ? new SeedItem(ItemId(categoryName, name), name, 1, basis)
            : new SeedItem(ItemId(categoryName, name), name, null);
}
