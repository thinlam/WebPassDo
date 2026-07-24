using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PassDo.Api.Contracts.Users;
using PassDo.Application.Common.Models;
using PassDo.Application.Users.Commands.UpdateCurrentUser;
using PassDo.Application.Users.Queries.GetCurrentUser;

namespace PassDo.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<object>>> GetMe()
    {
        var result = await _mediator.Send(new GetCurrentUserQuery());
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateMe([FromBody] UpdateUserRequest request)
    {
        var result = await _mediator.Send(new UpdateCurrentUserCommand(
            request.FullName,
            request.PhoneNumber,
            request.AvatarUrl));

        return Ok(ApiResponse<object>.Ok(result, "Profile updated."));
    }
}
