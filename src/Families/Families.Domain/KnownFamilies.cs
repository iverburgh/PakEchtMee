namespace Families.Domain;

/// <summary>Families that exist in every database because a migration creates them.</summary>
public static class KnownFamilies
{
    /// <summary>The first family; everything that existed before families were introduced was moved here.</summary>
    public static readonly Guid Verburgh = new("0196a3c2-5e10-7a41-9b2d-6f8e1c4d2b00");
}
