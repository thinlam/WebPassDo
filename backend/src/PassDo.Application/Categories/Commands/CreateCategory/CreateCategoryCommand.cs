using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Categories.DTOs;
using PassDo.Application.Categories.Mappings;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Domain.Entities;

namespace PassDo.Application.Categories.Commands.CreateCategory;

public record CreateCategoryCommand(
    string Name,
    string? Description,
    string? Slug,
    int DisplayOrder,
    bool IsActive) : IRequest<CategoryDto>;

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));

        RuleFor(x => x.Slug)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Slug));

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0);
    }
}

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly IApplicationDbContext _context;

    public CreateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slugify(name)
            : Slugify(request.Slug);

        var slugExists = await _context.Categories
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Slug == slug, cancellationToken);

        if (slugExists)
        {
            throw new ConflictException($"Category slug '{slug}' already exists.");
        }

        var category = new Category
        {
            Name = name,
            Description = request.Description?.Trim(),
            Slug = slug,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return CategoryMapper.ToDto(category);
    }

    private static string Slugify(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var c in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        var slug = builder.ToString().Normalize(NormalizationForm.FormC);
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", string.Empty);
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');
        return slug;
    }
}
