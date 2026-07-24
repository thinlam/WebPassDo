using PassDo.Application.Categories.DTOs;
using PassDo.Domain.Entities;

namespace PassDo.Application.Categories.Mappings;

public static class CategoryMapper
{
    public static CategoryDto ToDto(Category category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description,
        Slug = category.Slug,
        DisplayOrder = category.DisplayOrder,
        IsActive = category.IsActive
    };
}
