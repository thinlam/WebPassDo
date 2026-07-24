using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PassDo.Application.Chat;
using PassDo.Application.Common.Models;

namespace PassDo.Api.Controllers;

[ApiController]
[Route("api/conversations")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChatController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Preferred: product id in the route (avoids empty-body binding issues).</summary>
    [HttpPost("product/{productId:guid}")]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> StartOrGetByProduct(Guid productId)
    {
        var result = await _mediator.Send(new StartOrGetConversationCommand(productId));
        return Ok(ApiResponse<ConversationDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> StartOrGet(
        [FromBody] StartConversationRequest? request,
        [FromQuery] Guid? productId = null)
    {
        var id = request?.ProductId ?? productId ?? Guid.Empty;
        if (id == Guid.Empty)
        {
            return BadRequest(ApiResponse<ConversationDto>.Fail(
                "Thiếu productId. Dùng POST /api/conversations/product/{productId}."));
        }

        var result = await _mediator.Send(new StartOrGetConversationCommand(id));
        return Ok(ApiResponse<ConversationDto>.Ok(result));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ConversationDto>>>> GetMy()
    {
        var result = await _mediator.Send(new GetMyConversationsQuery());
        return Ok(ApiResponse<IReadOnlyList<ConversationDto>>.Ok(result));
    }

    [HttpGet("{id:guid}/messages")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MessageDto>>>> GetMessages(
        Guid id, [FromQuery] DateTime? after = null)
    {
        var result = await _mediator.Send(new GetConversationMessagesQuery(id, after));
        return Ok(ApiResponse<IReadOnlyList<MessageDto>>.Ok(result));
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<ActionResult<ApiResponse<MessageDto>>> SendMessage(Guid id, [FromBody] SendMessageRequest request)
    {
        var result = await _mediator.Send(new SendMessageCommand(id, request.Content));
        return Ok(ApiResponse<MessageDto>.Ok(result));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse<object>>> MarkRead(Guid id)
    {
        await _mediator.Send(new MarkConversationReadCommand(id));
        return Ok(ApiResponse<object>.Ok(new { }, "Marked as read."));
    }
}

public class StartConversationRequest
{
    public Guid ProductId { get; set; }
}

public class SendMessageRequest
{
    public string Content { get; set; } = string.Empty;
}
