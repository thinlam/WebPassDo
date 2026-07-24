using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PassDo.Application.Common.Models;
using PassDo.Application.Shipping;
using PassDo.Domain.Enums;

namespace PassDo.Api.Controllers;

[ApiController]
[Route("api/shipping")]
[Authorize]
public class ShippingController : ControllerBase
{
    private readonly IMediator _mediator;

    public ShippingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("calculate")]
    public async Task<ActionResult<ApiResponse<ShippingQuoteDto>>> Calculate([FromBody] CalculateShippingRequest request)
    {
        var result = await _mediator.Send(new CalculateShippingCommand(
            request.ProductId,
            request.PickupAddressId,
            request.DeliveryAddressId,
            request.DeliverySpeed));
        return Ok(ApiResponse<ShippingQuoteDto>.Ok(result));
    }
}

public record CalculateShippingRequest(
    Guid ProductId,
    Guid? PickupAddressId,
    Guid DeliveryAddressId,
    DeliverySpeed? DeliverySpeed);
