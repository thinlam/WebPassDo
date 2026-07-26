using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Addresses.DTOs;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Orders.Mappings;
using PassDo.Domain.Entities;
using PassDo.Domain.Enums;

namespace PassDo.Application.Addresses.Commands;

public record GetMyAddressesQuery() : IRequest<IReadOnlyList<UserAddressDto>>;
public record CreateAddressCommand(
    string RecipientName,
    string PhoneNumber,
    string Province,
    string District,
    string Ward,
    string StreetAddress,
    string? Note,
    AddressType AddressType,
    bool IsDefault,
    string? ProvinceCode = null,
    string? DistrictCode = null,
    string? WardCode = null) : IRequest<UserAddressDto>;
public record UpdateAddressCommand(
    Guid Id,
    string RecipientName,
    string PhoneNumber,
    string Province,
    string District,
    string Ward,
    string StreetAddress,
    string? Note,
    AddressType AddressType,
    bool IsDefault,
    string? ProvinceCode = null,
    string? DistrictCode = null,
    string? WardCode = null) : IRequest<UserAddressDto>;
public record DeleteAddressCommand(Guid Id) : IRequest;
public record SetDefaultAddressCommand(Guid Id) : IRequest<UserAddressDto>;

public class AddressCommandValidator : AbstractValidator<CreateAddressCommand>
{
    public AddressCommandValidator()
    {
        RuleFor(x => x.RecipientName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Province).NotEmpty().MaximumLength(100);
        RuleFor(x => x.District).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Ward).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StreetAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.AddressType).IsInEnum();
        RuleFor(x => x.ProvinceCode).MaximumLength(20);
        RuleFor(x => x.DistrictCode).MaximumLength(20);
        RuleFor(x => x.WardCode).MaximumLength(20);
    }
}

public class UpdateAddressCommandValidator : AbstractValidator<UpdateAddressCommand>
{
    public UpdateAddressCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RecipientName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PhoneNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Province).NotEmpty().MaximumLength(100);
        RuleFor(x => x.District).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Ward).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StreetAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.AddressType).IsInEnum();
        RuleFor(x => x.ProvinceCode).MaximumLength(20);
        RuleFor(x => x.DistrictCode).MaximumLength(20);
        RuleFor(x => x.WardCode).MaximumLength(20);
    }
}

public class AddressHandlers :
    IRequestHandler<GetMyAddressesQuery, IReadOnlyList<UserAddressDto>>,
    IRequestHandler<CreateAddressCommand, UserAddressDto>,
    IRequestHandler<UpdateAddressCommand, UserAddressDto>,
    IRequestHandler<DeleteAddressCommand>,
    IRequestHandler<SetDefaultAddressCommand, UserAddressDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public AddressHandlers(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<UserAddressDto>> Handle(GetMyAddressesQuery request, CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var items = await _context.UserAddresses.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return items.Select(ToDto).ToList();
    }

    public async Task<UserAddressDto> Handle(CreateAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        if (request.IsDefault || !await _context.UserAddresses.AnyAsync(x => x.UserId == userId, cancellationToken))
        {
            await ClearDefaults(userId, cancellationToken);
        }

        var entity = new UserAddress
        {
            UserId = userId,
            RecipientName = request.RecipientName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Province = request.Province.Trim(),
            District = request.District.Trim(),
            Ward = request.Ward.Trim(),
            ProvinceCode = string.IsNullOrWhiteSpace(request.ProvinceCode) ? null : request.ProvinceCode.Trim(),
            DistrictCode = string.IsNullOrWhiteSpace(request.DistrictCode) ? null : request.DistrictCode.Trim(),
            WardCode = string.IsNullOrWhiteSpace(request.WardCode) ? null : request.WardCode.Trim(),
            StreetAddress = request.StreetAddress.Trim(),
            Note = request.Note?.Trim(),
            AddressType = request.AddressType,
            IsDefault = request.IsDefault || !await _context.UserAddresses.AnyAsync(x => x.UserId == userId, cancellationToken)
        };

        _context.UserAddresses.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<UserAddressDto> Handle(UpdateAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var entity = await _context.UserAddresses.FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("UserAddress", request.Id);

        if (request.IsDefault)
        {
            await ClearDefaults(userId, cancellationToken);
        }

        entity.RecipientName = request.RecipientName.Trim();
        entity.PhoneNumber = request.PhoneNumber.Trim();
        entity.Province = request.Province.Trim();
        entity.District = request.District.Trim();
        entity.Ward = request.Ward.Trim();
        entity.ProvinceCode = string.IsNullOrWhiteSpace(request.ProvinceCode) ? null : request.ProvinceCode.Trim();
        entity.DistrictCode = string.IsNullOrWhiteSpace(request.DistrictCode) ? null : request.DistrictCode.Trim();
        entity.WardCode = string.IsNullOrWhiteSpace(request.WardCode) ? null : request.WardCode.Trim();
        entity.StreetAddress = request.StreetAddress.Trim();
        entity.Note = request.Note?.Trim();
        entity.AddressType = request.AddressType;
        entity.IsDefault = request.IsDefault;

        await _context.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task Handle(DeleteAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var entity = await _context.UserAddresses.FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("UserAddress", request.Id);

        _context.UserAddresses.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        if (entity.IsDefault)
        {
            var next = await _context.UserAddresses.Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);
            if (next is not null)
            {
                next.IsDefault = true;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public async Task<UserAddressDto> Handle(SetDefaultAddressCommand request, CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var entity = await _context.UserAddresses.FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("UserAddress", request.Id);

        await ClearDefaults(userId, cancellationToken);
        entity.IsDefault = true;
        await _context.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private async Task ClearDefaults(Guid userId, CancellationToken cancellationToken)
    {
        var defaults = await _context.UserAddresses.Where(x => x.UserId == userId && x.IsDefault).ToListAsync(cancellationToken);
        foreach (var item in defaults)
        {
            item.IsDefault = false;
        }
    }

    private Guid RequireUser()
        => _currentUser.UserId ?? throw new UnauthorizedException();

    private static UserAddressDto ToDto(UserAddress x) => new()
    {
        Id = x.Id,
        RecipientName = x.RecipientName,
        PhoneNumber = x.PhoneNumber,
        Province = x.Province,
        District = x.District,
        Ward = x.Ward,
        ProvinceCode = x.ProvinceCode,
        DistrictCode = x.DistrictCode,
        WardCode = x.WardCode,
        StreetAddress = x.StreetAddress,
        Note = x.Note,
        AddressType = x.AddressType.ToString(),
        IsDefault = x.IsDefault,
        FullAddress = OrderMapper.FormatAddress(x.StreetAddress, x.Ward, x.District, x.Province)
    };
}
