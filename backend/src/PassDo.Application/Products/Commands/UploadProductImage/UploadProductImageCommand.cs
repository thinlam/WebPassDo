using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Products.DTOs;
using PassDo.Application.Products.Mappings;
using PassDo.Domain.Constants;
using PassDo.Domain.Entities;

namespace PassDo.Application.Products.Commands.UploadProductImage;

public record UploadProductImageCommand(
    Guid ProductId,
    Stream FileStream,
    string FileName,
    string ContentType,
    bool SetAsPrimary) : IRequest<ProductImageDto>;

public class UploadProductImageCommandValidator : AbstractValidator<UploadProductImageCommand>
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    public UploadProductImageCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .Must(x => AllowedContentTypes.Contains(x))
            .WithMessage("Only jpeg, png, webp, and gif images are allowed.");
        RuleFor(x => x.FileStream)
            .NotNull()
            .Must(x => x.CanRead)
            .WithMessage("File stream is required.");
    }
}

public class UploadProductImageCommandHandler : IRequestHandler<UploadProductImageCommand, ProductImageDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorageService _fileStorageService;

    public UploadProductImageCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFileStorageService fileStorageService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _fileStorageService = fileStorageService;
    }

    public async Task<ProductImageDto> Handle(UploadProductImageCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var product = await _context.Products
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product", request.ProductId);
        }

        var isOwner = _currentUserService.UserId == product.SellerId;
        var isAdmin = string.Equals(_currentUserService.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase);

        if (!isOwner && !isAdmin)
        {
            throw new ForbiddenException("You can only upload images for your own products.");
        }

        var url = await _fileStorageService.UploadAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            cancellationToken);

        var setAsPrimary = request.SetAsPrimary || product.Images.Count == 0;

        if (setAsPrimary)
        {
            foreach (var existing in product.Images)
            {
                existing.IsPrimary = false;
            }
        }

        var image = new ProductImage
        {
            ProductId = product.Id,
            Url = url,
            FileName = request.FileName,
            IsPrimary = setAsPrimary,
            DisplayOrder = product.Images.Count == 0
                ? 0
                : product.Images.Max(x => x.DisplayOrder) + 1
        };

        _context.ProductImages.Add(image);
        await _context.SaveChangesAsync(cancellationToken);

        return ProductMapper.ToImageDto(image);
    }
}
