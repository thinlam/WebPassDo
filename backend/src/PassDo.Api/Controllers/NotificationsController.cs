using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PassDo.Application.Common.Models;
using PassDo.Application.Notifications.Commands;
using PassDo.Application.Notifications.Queries;

namespace PassDo.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetNotificationsQuery(page, pageSize));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<object>>> GetUnreadCount()
    {
        var result = await _mediator.Send(new GetUnreadNotificationCountQuery());
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkRead(Guid id)
    {
        await _mediator.Send(new MarkNotificationReadCommand(id));
        return Ok(ApiResponse<object>.Ok(new { }, "Notification marked as read."));
    }

    [HttpPost("read-all")]
    public async Task<ActionResult<ApiResponse<object>>> MarkAllRead()
    {
        await _mediator.Send(new MarkAllNotificationsReadCommand());
        return Ok(ApiResponse<object>.Ok(new { }, "All notifications marked as read."));
    }
}
