using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Users.DTOs;
using PassDo.Application.Users.Mappings;

namespace PassDo.Application.Users.Queries.GetCurrentUser;

public record GetCurrentUserQuery : IRequest<UserDto>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == _currentUserService.UserId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("User", _currentUserService.UserId.Value);
        }

        return UserMapper.ToDto(user);
    }
}
