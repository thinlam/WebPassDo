using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Products.DTOs;
using PassDo.Application.Products.Mappings;
using PassDo.Domain.Constants;

namespace PassDo.Application.Products.Commands.SetPrimaryProductImage;

public record SetPrimaryProductImageCommand(Guid ProductId, Guid ImageId) : IRequest<ProductImageDto>;

public class SetPrimaryProductImageCommandHandler : IRequestHandler<SetPrimaryProductImageCommand, ProductImageDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public SetPrimaryProductImageCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ProductImageDto> Handle(SetPrimaryProductImageCommand request, CancellationToken cancellationToken)
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
            throw new ForbiddenException("You can only update images of your own products.");
        }

        var image = product.Images.FirstOrDefault(x => x.Id == request.ImageId);
        if (image is null)
        {
            throw new NotFoundException("ProductImage", request.ImageId);
        }

        foreach (var existing in product.Images)
        {
            existing.IsPrimary = existing.Id == image.Id;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ProductMapper.ToImageDto(image);
    }
}
