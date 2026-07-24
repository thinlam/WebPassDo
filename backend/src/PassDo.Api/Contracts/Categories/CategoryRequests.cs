namespace PassDo.Api.Contracts.Categories;

public record CreateCategoryRequest(
    string Name,
    string? Description,
    string? Slug,
    int DisplayOrder = 0,
    bool IsActive = true);

public record UpdateCategoryRequest(
    string Name,
    string? Description,
    string? Slug,
    int DisplayOrder,
    bool IsActive);
