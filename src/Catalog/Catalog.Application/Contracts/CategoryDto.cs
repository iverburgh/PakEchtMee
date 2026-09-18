using Catalog.Domain.Categories;

namespace Catalog.Application.Contracts;

public sealed record CategoryDto(Guid Id, string Name, bool IsDeleted)
{
    public static CategoryDto From(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);

        return new CategoryDto(category.Id, category.Name.Value, category.IsDeleted);
    }
}
