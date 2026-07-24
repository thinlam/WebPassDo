using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.BankAccounts.DTOs;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Orders.Mappings;
using PassDo.Domain.Entities;

namespace PassDo.Application.BankAccounts.Commands;

public record GetMyBankAccountsQuery() : IRequest<IReadOnlyList<UserBankAccountDto>>;
public record CreateBankAccountCommand(
    string BankName,
    string AccountNumber,
    string AccountHolderName,
    string? Branch,
    bool IsDefault) : IRequest<UserBankAccountDto>;
public record UpdateBankAccountCommand(
    Guid Id,
    string BankName,
    string AccountNumber,
    string AccountHolderName,
    string? Branch,
    bool IsDefault) : IRequest<UserBankAccountDto>;
public record DeleteBankAccountCommand(Guid Id) : IRequest;
public record SetDefaultBankAccountCommand(Guid Id) : IRequest<UserBankAccountDto>;

public class CreateBankAccountCommandValidator : AbstractValidator<CreateBankAccountCommand>
{
    public CreateBankAccountCommandValidator()
    {
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.AccountNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AccountHolderName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Branch).MaximumLength(200);
    }
}

public class UpdateBankAccountCommandValidator : AbstractValidator<UpdateBankAccountCommand>
{
    public UpdateBankAccountCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.AccountNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AccountHolderName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Branch).MaximumLength(200);
    }
}

public class BankAccountHandlers :
    IRequestHandler<GetMyBankAccountsQuery, IReadOnlyList<UserBankAccountDto>>,
    IRequestHandler<CreateBankAccountCommand, UserBankAccountDto>,
    IRequestHandler<UpdateBankAccountCommand, UserBankAccountDto>,
    IRequestHandler<DeleteBankAccountCommand>,
    IRequestHandler<SetDefaultBankAccountCommand, UserBankAccountDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public BankAccountHandlers(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<UserBankAccountDto>> Handle(GetMyBankAccountsQuery request, CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var items = await _context.UserBankAccounts.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
        return items.Select(ToDto).ToList();
    }

    public async Task<UserBankAccountDto> Handle(CreateBankAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var isFirst = !await _context.UserBankAccounts.AnyAsync(x => x.UserId == userId, cancellationToken);
        if (request.IsDefault || isFirst)
        {
            await ClearDefaults(userId, cancellationToken);
        }

        var entity = new UserBankAccount
        {
            UserId = userId,
            BankName = request.BankName.Trim(),
            AccountNumber = request.AccountNumber.Trim(),
            AccountHolderName = request.AccountHolderName.Trim(),
            Branch = request.Branch?.Trim(),
            IsDefault = request.IsDefault || isFirst
        };

        _context.UserBankAccounts.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task<UserBankAccountDto> Handle(UpdateBankAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var entity = await _context.UserBankAccounts.FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("UserBankAccount", request.Id);

        if (request.IsDefault)
        {
            await ClearDefaults(userId, cancellationToken);
        }

        entity.BankName = request.BankName.Trim();
        entity.AccountNumber = request.AccountNumber.Trim();
        entity.AccountHolderName = request.AccountHolderName.Trim();
        entity.Branch = request.Branch?.Trim();
        entity.IsDefault = request.IsDefault;
        await _context.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    public async Task Handle(DeleteBankAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var entity = await _context.UserBankAccounts.FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("UserBankAccount", request.Id);

        var inUse = await _context.Products.AnyAsync(x => x.BankAccountId == entity.Id, cancellationToken);
        if (inUse)
        {
            throw new ConflictException("Cannot delete a bank account that is linked to products.");
        }

        _context.UserBankAccounts.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        if (entity.IsDefault)
        {
            var next = await _context.UserBankAccounts.Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);
            if (next is not null)
            {
                next.IsDefault = true;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public async Task<UserBankAccountDto> Handle(SetDefaultBankAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var entity = await _context.UserBankAccounts.FirstOrDefaultAsync(x => x.Id == request.Id && x.UserId == userId, cancellationToken)
            ?? throw new NotFoundException("UserBankAccount", request.Id);

        await ClearDefaults(userId, cancellationToken);
        entity.IsDefault = true;
        await _context.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private async Task ClearDefaults(Guid userId, CancellationToken cancellationToken)
    {
        var defaults = await _context.UserBankAccounts.Where(x => x.UserId == userId && x.IsDefault).ToListAsync(cancellationToken);
        foreach (var item in defaults)
        {
            item.IsDefault = false;
        }
    }

    private Guid RequireUser() => _currentUser.UserId ?? throw new UnauthorizedException();

    private static UserBankAccountDto ToDto(UserBankAccount x) => new()
    {
        Id = x.Id,
        BankName = x.BankName,
        AccountNumber = x.AccountNumber,
        AccountNumberMasked = OrderMapper.MaskAccountNumber(x.AccountNumber),
        AccountHolderName = x.AccountHolderName,
        Branch = x.Branch,
        IsDefault = x.IsDefault
    };
}
