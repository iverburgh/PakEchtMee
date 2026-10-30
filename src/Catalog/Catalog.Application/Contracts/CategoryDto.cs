using Catalog.Domain.Categories;

namespace Catalog.Application.Contracts;

/// <summary><c>IsSwitchedOn</c>: for a general category, whether the caller's family uses it. Own categories are always on.</summary>
public sealed record CategoryDto(Guid Id, string Name, bool IsDeleted, bool IsGeneral, bool IsSwitchedOn)
{
    public static CategoryDto From(Category category, bool isSwitchedOn = true)
    {
        ArgumentNullException.ThrowIfNull(category);

        return new CategoryDto(category.Id, category.Name.Value, category.IsDeleted, category.IsGeneral, isSwitchedOn);
    }
}
