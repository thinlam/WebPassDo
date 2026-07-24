using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Domain.Constants;
using PassDo.Domain.Enums;

namespace PassDo.Application.Products.Commands.DeleteProduct;

public record DeleteProductCommand(Guid Id) : IRequest;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorageService _fileStorageService;

    public DeleteProductCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFileStorageService fileStorageService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _fileStorageService = fileStorageService;
    }

    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var product = await _context.Products
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Product", request.Id);
        }

        var isOwner = _currentUserService.UserId == product.SellerId;
        var isAdmin = string.Equals(_currentUserService.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase);

        if (!isOwner && !isAdmin)
        {
            throw new ForbiddenException("You can only delete your own products.");
        }

        if (product.Status is ProductStatus.Reserved)
        {
            throw new ConflictException("Cannot delete a reserved product.");
        }

        var hasActiveOrders = await _context.Orders.AnyAsync(
            x => x.ProductId == product.Id
                 && PassDo.Application.Orders.DTOs.OrderStatusGroups.ActiveProcessing.Contains(x.Status),
            cancellationToken);

        if (hasActiveOrders)
        {
            throw new ConflictException("Cannot delete a product that has active orders.");
        }

        foreach (var image in product.Images.ToList())
        {
            await _fileStorageService.DeleteAsync(image.Url, cancellationToken);
            _context.ProductImages.Remove(image);
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
