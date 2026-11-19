using Catalog.Domain.Items;

namespace Catalog.Infrastructure.Sql.Seeding;

public sealed record SeedItem(
    Guid Id,
    string Name,
    int? Quantity,
    QuantityBasis Basis = QuantityBasis.Fixed,
    QuantityPeriod Period = QuantityPeriod.PerTrip,
    string? Unit = null);

/// <summary>How a seeded item is counted on a trip: a quantity with an optional unit, per head, and once or per day or night.</summary>
public sealed record SeedRule(
    QuantityBasis Basis,
    QuantityPeriod Period = QuantityPeriod.PerTrip,
    int Quantity = 1,
    string? Unit = null);

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
    /// Items packed for every child, adult or person on a trip, once or for every day. The sheet's "3x" entries were
    /// three children, so they are per child here; underwear and socks are needed every day. Declared before
    /// <see cref="Categories"/>, which reads it while it is initialised.
    /// </summary>
    public static readonly IReadOnlyDictionary<(string Category, string Item), SeedRule> PerHead =
        new Dictionary<(string Category, string Item), SeedRule>
        {
            [(KinderenSlapen, "Slaapzak")] = new(QuantityBasis.PerChild),
            [(KinderenSlapen, "Matje")] = new(QuantityBasis.PerChild),
            [(KinderenSlapen, "Kussen")] = new(QuantityBasis.PerChild),
            [(KinderenToiletartikelen, "Handdoek(en) douchen")] = new(QuantityBasis.PerChild),
            [(KinderenBuitenkleding, "Jassen")] = new(QuantityBasis.PerChild),
            [(KinderenBuitenkleding, "Laarzen")] = new(QuantityBasis.PerChild),
            [(KinderenBuitenkleding, "Schoenen")] = new(QuantityBasis.PerChild),
            [(KinderenBuitenkleding, "Crocs/sandalen/slippers")] = new(QuantityBasis.PerChild),
            [(KinderenBuitenkleding, "Petten")] = new(QuantityBasis.PerChild),
            [(KinderenBuitenkleding, "Zonnebrillen")] = new(QuantityBasis.PerChild),
            [(KinderenVermaak, "Zwemkleding")] = new(QuantityBasis.PerChild),
            [(KinderenVermaak, "Handdoek(en) zwemmen")] = new(QuantityBasis.PerChild),
            [(Buitenkleding, "Jas")] = new(QuantityBasis.PerAdult),
            [(Buitenkleding, "Slippers/crocs/teva's")] = new(QuantityBasis.PerAdult),
            [(Buitenkleding, "Pet")] = new(QuantityBasis.PerAdult),
            [(Buitenkleding, "Zonnebrillen")] = new(QuantityBasis.PerAdult),
            [(Buitenkleding, "Wandelschoenen")] = new(QuantityBasis.PerAdult),
            [(Toiletartikelen, "Handdoeken douchen")] = new(QuantityBasis.PerAdult),
            [(SlapenInTent, "Matjes")] = new(QuantityBasis.PerAdult),
            [(SlapenInTent, "Kussens")] = new(QuantityBasis.PerAdult),
            [(Vermaak, "Badhanddoeken")] = new(QuantityBasis.PerPerson),
            [(Algemeen, "Geldige paspoorten")] = new(QuantityBasis.PerPerson),
            [(Kampeerkeuken, "Borden")] = new(QuantityBasis.PerPerson),
            [(Kampeerkeuken, "Bekers")] = new(QuantityBasis.PerPerson),
            [(Kampeerkeuken, "Bestek")] = new(QuantityBasis.PerPerson),
            [(KamperenAlgemeen, "Stoelen")] = new(QuantityBasis.PerPerson),
            [(Kleding, "Onderbroeken")] = new(QuantityBasis.PerAdult, QuantityPeriod.PerDay),
            [(Kleding, "Sokken")] = new(QuantityBasis.PerAdult, QuantityPeriod.PerDay),
            [(KinderenKleding, "Sokken")] = new(QuantityBasis.PerChild, QuantityPeriod.PerDay),
        };

    /// <summary>
    /// The rule of every other seeded item, so no seeded item is without an amount. Kept apart from <see cref="PerHead"/>
    /// because existing databases receive these in a later upgrade than those. Declared before <see cref="Categories"/>,
    /// which reads it while it is initialised.
    /// </summary>
    public static readonly IReadOnlyDictionary<(string Category, string Item), SeedRule> SeedRules =
        new Dictionary<(string Category, string Item), SeedRule>
        {
            [(KinderenAlgemeen, "Snoetenpoetsers")] = new(QuantityBasis.PerChild, QuantityPeriod.PerTrip, 1, "pak"),
            [(KinderenAlgemeen, "Extra beddengoed")] = new(QuantityBasis.PerChild, QuantityPeriod.PerTrip, 1, "set"),
            [(KinderenAlgemeen, "Dekens")] = new(QuantityBasis.PerChild),
            [(KinderenToiletartikelen, "Vitamine D")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "flesje"),
            [(KinderenToiletartikelen, "Nagelknippertjes")] = new(QuantityBasis.Fixed),
            [(KinderenToiletartikelen, "Tandenborstel(s) + tandpasta")] = new(QuantityBasis.PerChild),
            [(KinderenToiletartikelen, "Zalf en cremes")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "set"),
            [(KinderenSlapen, "Knuffels")] = new(QuantityBasis.PerChild),
            [(KinderenSlapen, "Pyjama's")] = new(QuantityBasis.PerChild, QuantityPeriod.PerTrip, 2),
            [(KinderenSlapen, "Badjas")] = new(QuantityBasis.PerChild),
            [(KinderenSlapen, "Wekker")] = new(QuantityBasis.Fixed),
            [(KinderenBuitenkleding, "Paraplu")] = new(QuantityBasis.PerChild),
            [(KinderenKleding, "Lange broeken/ korte broeken")] = new(QuantityBasis.PerChild, QuantityPeriod.PerTrip, 4),
            [(KinderenKleding, "Shirts korte & lange mouwen")] = new(QuantityBasis.PerChild, QuantityPeriod.PerDay),
            [(KinderenKleding, "Truien")] = new(QuantityBasis.PerChild, QuantityPeriod.PerTrip, 2),
            [(KinderenKleding, "Jurken")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 3),
            [(KinderenVermaak, "Speelgoed")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "tas"),
            [(KinderenVermaak, "Duikringen")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "set"),
            [(KinderenVermaak, "Boekjes")] = new(QuantityBasis.PerChild, QuantityPeriod.PerTrip, 2),
            [(KinderenVermaak, "Opblaasband")] = new(QuantityBasis.Fixed),
            [(KinderenVermaak, "Zwembandjes")] = new(QuantityBasis.PerChild, QuantityPeriod.PerTrip, 1, "paar"),
            [(KinderenVermaak, "Drijfpakje")] = new(QuantityBasis.Fixed),
            [(KinderenVermaak, "Potloden + puntenslijper")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "set"),
            [(KinderenVermaak, "Speelboeken")] = new(QuantityBasis.PerChild),
            [(KinderenVermaak, "Tafeltennisspullen")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "set"),
            [(KinderenVermaak, "Badminton rackets")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "set"),
            [(KinderenVermaak, "Klitterband bal")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "set"),
            [(KinderenVermaak, "Zwembrillen")] = new(QuantityBasis.PerChild),
            [(KinderenVermaak, "Zeemeerminstaarten")] = new(QuantityBasis.PerChild),
            [(KinderenVermaak, "Waterspuiten")] = new(QuantityBasis.PerChild),
            [(Kleding, "T-shirts")] = new(QuantityBasis.PerAdult, QuantityPeriod.PerDay),
            [(Kleding, "Korte + lange broeken")] = new(QuantityBasis.PerAdult, QuantityPeriod.PerTrip, 3),
            [(Kleding, "Vest")] = new(QuantityBasis.PerAdult),
            [(KledingMartine, "BH's")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 3),
            [(KledingMartine, "Jurk + ondergoed")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 2),
            [(KledingMartine, "Badjas")] = new(QuantityBasis.Fixed),
            [(Buitenkleding, "Paraplu")] = new(QuantityBasis.PerAdult),
            [(Toiletartikelen, "Vitaminepillen")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "potje"),
            [(Toiletartikelen, "Scheermesje")] = new(QuantityBasis.Fixed),
            [(Toiletartikelen, "Oordoppen Martine")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "doosje"),
            [(Toiletartikelen, "Shampoo")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "fles"),
            [(Toiletartikelen, "Tandenborstel + pasta")] = new(QuantityBasis.PerAdult),
            [(Toiletartikelen, "Listerine")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "fles"),
            [(Toiletartikelen, "Nagelknippers")] = new(QuantityBasis.Fixed),
            [(Toiletartikelen, "Tekentang")] = new(QuantityBasis.Fixed),
            [(Toiletartikelen, "Antimuggenspul")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "fles"),
            [(Toiletartikelen, "Blistex")] = new(QuantityBasis.PerAdult),
            [(ToiletartikelenMartine, "Conditioner")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "fles"),
            [(ToiletartikelenMartine, "Borstel")] = new(QuantityBasis.Fixed),
            [(ToiletartikelenMartine, "Tampons")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "doos"),
            [(ToiletartikelenMartine, "Scrub / tea tree / sudocrem")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "set"),
            [(Vermaak, "Zwemkleding I & M")] = new(QuantityBasis.PerAdult),
            [(Vermaak, "Sportspullen")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "set"),
            [(Vermaak, "Boeken / E-reader")] = new(QuantityBasis.PerAdult),
            [(Vermaak, "Schaakspel")] = new(QuantityBasis.Fixed),
            [(Vermaak, "Spelletjes")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 2),
            [(VervoerZonderAuto, "Rugzakken")] = new(QuantityBasis.PerPerson),
            [(Algemeen, "Pen & notitieblokje")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "set"),
            [(Algemeen, "Geldige creditcard")] = new(QuantityBasis.Fixed),
            [(Algemeen, "Portemonnees")] = new(QuantityBasis.PerAdult),
            [(Algemeen, "Opgeladen powerbank")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 2),
            [(Algemeen, "Telefoonladers")] = new(QuantityBasis.PerAdult),
            [(Algemeen, "Horlogeladers")] = new(QuantityBasis.Fixed),
            [(Algemeen, "Huissleutel")] = new(QuantityBasis.Fixed),
            [(Algemeen, "Reservesleutel auto")] = new(QuantityBasis.Fixed),
            [(Algemeen, "EHBO-set")] = new(QuantityBasis.Fixed),
            [(Algemeen, "Zonnebrand")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 2, "fles"),
            [(Algemeen, "Zakdoekjes")] = new(QuantityBasis.PerPerson, QuantityPeriod.PerTrip, 1, "pak"),
            [(Algemeen, "Shoppers")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 3),
            [(Algemeen, "Boodschappenkratten")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 2),
            [(Algemeen, "Vuile waszak")] = new(QuantityBasis.Fixed),
            [(Algemeen, "Kleine stuffbags")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 4),
            [(Algemeen, "Waterflesjes / Dopper")] = new(QuantityBasis.PerPerson),
            [(Algemeen, "Cash")] = new(QuantityBasis.Fixed),
            [(Algemeen, "Oordopjes voor muziek")] = new(QuantityBasis.PerAdult),
            [(Keukengerei, "Afwasmiddel")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "fles"),
            [(Keukengerei, "Theedoeken")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 3),
            [(Keukengerei, "Doekjes")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 3),
            [(Keukengerei, "Tupperware bakjes")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 4),
            [(Koken, "Kruiden")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "set"),
            [(Koken, "Olijfolie")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "fles"),
            [(Tent, "Tent + pomp")] = new(QuantityBasis.Fixed),
            [(Tent, "Haringen")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 30),
            [(Tent, "Tarp")] = new(QuantityBasis.Fixed),
            [(Tent, "Tenthamer")] = new(QuantityBasis.Fixed),
            [(Tent, "Veger en blik")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "set"),
            [(Tent, "Inloop/modderdoek")] = new(QuantityBasis.Fixed),
            [(SlapenInTent, "Beddengoed")] = new(QuantityBasis.PerAdult, QuantityPeriod.PerTrip, 1, "set"),
            [(SlapenInTent, "Matrashoes")] = new(QuantityBasis.PerAdult),
            [(Kampeerkeuken, "Keukenkastje")] = new(QuantityBasis.Fixed),
            [(Kampeerkeuken, "Koelbox + elementen")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "set"),
            [(Kampeerkeuken, "Vuilniszakken")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "rol"),
            [(Kampeerkeuken, "Brander")] = new(QuantityBasis.Fixed),
            [(Kampeerkeuken, "Brandstof")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 2, "bus"),
            [(Kampeerkeuken, "Lucifers + aansteker")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "set"),
            [(Kampeerkeuken, "Pannen")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 3),
            [(Kampeerkeuken, "Spatel")] = new(QuantityBasis.Fixed),
            [(Kampeerkeuken, "Schaar")] = new(QuantityBasis.Fixed),
            [(Kampeerkeuken, "Handmixer")] = new(QuantityBasis.Fixed),
            [(Kampeerkeuken, "Broodmes")] = new(QuantityBasis.Fixed),
            [(Kampeerkeuken, "Maatbeker")] = new(QuantityBasis.Fixed),
            [(Kampeerkeuken, "Opinel zakmes")] = new(QuantityBasis.Fixed),
            [(Kampeerkeuken, "Snijplank")] = new(QuantityBasis.Fixed),
            [(Kampeerkeuken, "Keukenmes")] = new(QuantityBasis.Fixed),
            [(Kampeerkeuken, "Opvouwvergiet")] = new(QuantityBasis.Fixed),
            [(Afwas, "Sponsje + borstel")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "set"),
            [(Afwas, "Emmer")] = new(QuantityBasis.Fixed),
            [(Afwas, "Afwasmiddel")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "fles"),
            [(KamperenAlgemeen, "Tafel")] = new(QuantityBasis.Fixed),
            [(KamperenAlgemeen, "WC-papier")] = new(QuantityBasis.Fixed, QuantityPeriod.PerDay, 1, "rol"),
            [(KamperenAlgemeen, "Ducttape")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "rol"),
            [(KamperenAlgemeen, "Zwitsers zakmes")] = new(QuantityBasis.Fixed),
            [(KamperenAlgemeen, "Leatherman zakmes")] = new(QuantityBasis.Fixed),
            [(KamperenAlgemeen, "Lampen")] = new(QuantityBasis.PerPerson),
            [(KamperenAlgemeen, "Plasemmer")] = new(QuantityBasis.Fixed),
            [(KamperenAlgemeen, "Hangmat + standaard")] = new(QuantityBasis.Fixed),
            [(Was, "Waslijn")] = new(QuantityBasis.Fixed),
            [(Was, "Touw")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "rol"),
            [(Was, "Knijpers")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 20),
            [(Was, "Wasmiddel")] = new(QuantityBasis.Fixed, QuantityPeriod.PerTrip, 1, "fles"),
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
        => PerHead.TryGetValue((categoryName, name), out SeedRule? rule) || SeedRules.TryGetValue((categoryName, name), out rule)
            ? new SeedItem(ItemId(categoryName, name), name, rule.Quantity, rule.Basis, rule.Period, rule.Unit)
            : new SeedItem(ItemId(categoryName, name), name, null);
}
