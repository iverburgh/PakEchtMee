namespace Families.Application.Contracts;

/// <summary>The family an account belongs to; the API resolves this on every request to decide what the caller may see.</summary>
public sealed record MembershipDto(Guid FamilyId, string FamilyName);
