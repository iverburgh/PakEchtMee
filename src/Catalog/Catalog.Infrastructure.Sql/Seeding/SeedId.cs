using System.Security.Cryptography;
using System.Text;

namespace Catalog.Infrastructure.Sql.Seeding;

/// <summary>
/// Name-based ids (RFC 9562 version 5) for seed records. The same key always yields the same id on every machine,
/// so seeding can recognise its own records without storing a marker, while the seed definition stays free of GUID literals.
/// </summary>
public static class SeedId
{
    private static readonly Guid Namespace = new("6b1f3c0e-2d4a-4f57-9a8e-5c1d7e2b9f40");

    public static Guid For(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        byte[] input = [.. Namespace.ToByteArray(bigEndian: true), .. Encoding.UTF8.GetBytes(key)];

        // SHA-1 is what version 5 prescribes; it derives an identifier here and protects nothing.
#pragma warning disable CA5350
        byte[] hash = SHA1.HashData(input);
#pragma warning restore CA5350

        hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);

        return new Guid(hash.AsSpan(0, 16), bigEndian: true);
    }
}
