using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Products.DTOs;
using PassDo.Application.Products.Mappings;
using PassDo.Domain.Constants;
using PassDo.Domain.Enums;

namespace PassDo.Application.Products.Commands.UpdateProductStatus;

public record UpdateProductStatusCommand(Guid Id, ProductStatus Status) : IRequest<ProductDto>;

public class UpdateProductStatusCommandValidator : AbstractValidator<UpdateProductStatusCommand>
{
    public UpdateProductStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class UpdateProductStatusCommandHandler : IRequestHandler<UpdateProductStatusCommand, ProductDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateProductStatusCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<ProductDto> Handle(UpdateProductStatusCommand request, CancellationToken cancellationToken)
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
            throw new ForbiddenException("You can only update status of your own products.");
        }

        if (product.Status == ProductStatus.Sold && request.Status != ProductStatus.Sold)
        {
            throw new ConflictException("Sold products cannot change status.");
        }

        if (!isAdmin && request.Status is ProductStatus.Rejected)
        {
            throw new ForbiddenException("Only admin can reject products.");
        }

        if (request.Status is ProductStatus.Reserved or ProductStatus.Sold)
        {
            throw new ConflictException("Reserved/Sold status is managed by order flow.");
        }

        product.Status = request.Status;
        await _context.SaveChangesAsync(cancellationToken);

        var updated = await _context.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Images)
            .FirstAsync(x => x.Id == product.Id, cancellationToken);

        var seller = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == updated.SellerId, cancellationToken);

        var dto = ProductMapper.ToDto(updated);
        dto.SellerName = seller?.FullName;
        return dto;
    }
}
