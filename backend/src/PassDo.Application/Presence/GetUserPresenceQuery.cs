using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;

namespace PassDo.Application.Presence;

public record GetUserPresenceQuery(Guid UserId) : IRequest<PresenceDto>;

public class GetUserPresenceQueryHandler : IRequestHandler<GetUserPresenceQuery, PresenceDto>
{
    private readonly IApplicationDbContext _context;

    public GetUserPresenceQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PresenceDto> Handle(GetUserPresenceQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("User", request.UserId);
        }

        return new PresenceDto
        {
            IsOnline = PresenceRules.IsOnline(user.LastSeenAt, DateTime.UtcNow),
            LastSeenAt = user.LastSeenAt
        };
    }
}

