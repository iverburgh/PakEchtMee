using Shared.Validation;

namespace Families.Domain;

/// <summary>
/// A family: the unit that owns a catalog and trips. Membership changes go through here so a family never lists
/// the same address twice; that an address is in at most one family is checked by the application and the database.
/// </summary>
public sealed class Family
{
    private readonly List<Member> members = [];

    private Family(Guid id, FamilyName name)
    {
        Id = id;
        Name = name;
        Composition = FamilyComposition.Default;
    }

    private Family()
    {
        Name = null!;
        Composition = null!;
    }

    public Guid Id { get; private set; }

    public FamilyName Name { get; private set; }

    public FamilyComposition Composition { get; private set; }

    public IReadOnlyList<Member> Members => members;

    public static Family Create(FamilyName name) => Create(Guid.CreateVersion7(), name);

    /// <summary>Creates a family with a caller-chosen id, for families other code refers to by a well-known id.</summary>
    public static Family Create(Guid id, FamilyName name)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(id, Guid.Empty);
        ArgumentNullException.ThrowIfNull(name);

        return new Family(id, name);
    }

    public void Rename(FamilyName name)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
    }

    /// <summary>Changes the family's size; existing trips keep their own composition.</summary>
    public void ChangeComposition(FamilyComposition composition)
    {
        ArgumentNullException.ThrowIfNull(composition);

        Composition = composition;
    }

    public Member AddMember(EmailAddress email)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (HasMember(email))
            throw new DomainValidationException($"{email} is al lid van {Name}.");

        Member member = Member.Create(Id, email);
        members.Add(member);

        return member;
    }

    public void RemoveMember(EmailAddress email)
    {
        ArgumentNullException.ThrowIfNull(email);

        Member member = members.Find(member => member.Email == email)
            ?? throw new NotFoundException($"{email} is geen lid van {Name}.");

        members.Remove(member);
    }

    public bool HasMember(EmailAddress email) => members.Exists(member => member.Email == email);
}
