namespace Families.Domain;

/// <summary>A Google account that belongs to a family. The address is the identity; it is unique across all families.</summary>
public sealed class Member
{
    private Member(Guid familyId, EmailAddress email)
    {
        FamilyId = familyId;
        Email = email;
    }

    private Member() => Email = null!;

    public Guid FamilyId { get; private set; }

    public EmailAddress Email { get; private set; }

    internal static Member Create(Guid familyId, EmailAddress email) => new(familyId, email);
}
