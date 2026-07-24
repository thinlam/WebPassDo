using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Domain.Constants;

namespace PassDo.Application.Products.Commands.DeleteProductImage;

public record DeleteProductImageCommand(Guid ProductId, Guid ImageId) : IRequest;

public class DeleteProductImageCommandHandler : IRequestHandler<DeleteProductImageCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorageService _fileStorageService;

    public DeleteProductImageCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFileStorageService fileStorageService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _fileStorageService = fileStorageService;
    }

    public async Task Handle(DeleteProductImageCommand request, CancellationToken cancellationToken)
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
            throw new ForbiddenException("You can only delete images of your own products.");
        }

        var image = product.Images.FirstOrDefault(x => x.Id == request.ImageId);
        if (image is null)
        {
            throw new NotFoundException("ProductImage", request.ImageId);
        }

        await _fileStorageService.DeleteAsync(image.Url, cancellationToken);
        _context.ProductImages.Remove(image);

        if (image.IsPrimary)
        {
            var nextPrimary = product.Images
                .Where(x => x.Id != image.Id)
                .OrderBy(x => x.DisplayOrder)
                .FirstOrDefault();

            if (nextPrimary is not null)
            {
                nextPrimary.IsPrimary = true;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
