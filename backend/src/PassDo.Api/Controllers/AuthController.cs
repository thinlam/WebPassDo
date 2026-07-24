using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PassDo.Api.Contracts.Auth;
using PassDo.Application.Auth.Commands.Login;
using PassDo.Application.Auth.Commands.Logout;
using PassDo.Application.Auth.Commands.RefreshToken;
using PassDo.Application.Auth.Commands.Register;
using PassDo.Application.Common.Models;

namespace PassDo.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> Register([FromBody] RegisterRequest request)
    {
        var result = await _mediator.Send(new RegisterCommand(
            request.Email,
            request.Password,
            request.FullName,
            request.PhoneNumber,
            GetClientIp()));

        return Ok(ApiResponse<object>.Ok(result, "Registration successful."));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> Login([FromBody] LoginRequest request)
    {
        var result = await _mediator.Send(new LoginCommand(
            request.Email,
            request.Password,
            GetClientIp()));

        return Ok(ApiResponse<object>.Ok(result, "Login successful."));
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _mediator.Send(new RefreshTokenCommand(
            request.RefreshToken,
            GetClientIp()));

        return Ok(ApiResponse<object>.Ok(result, "Token refreshed."));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] LogoutRequest request)
    {
        await _mediator.Send(new LogoutCommand(request.RefreshToken));
        return Ok(ApiResponse<object>.Ok(null!, "Logout successful."));
    }

    private string? GetClientIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();
}
