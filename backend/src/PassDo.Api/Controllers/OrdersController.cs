using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PassDo.Api.Contracts.Orders;
using PassDo.Application.Common.Models;
using PassDo.Application.Orders.Commands.CreateOrder;
using PassDo.Application.Orders.Commands.OrderActions;
using PassDo.Application.Orders.Commands.PreviewOrder;
using PassDo.Application.Orders.Queries.GetMyPurchases;
using PassDo.Application.Orders.Queries.GetMySales;
using PassDo.Application.Orders.Queries.GetOrderById;
using PassDo.Domain.Enums;

namespace PassDo.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("preview")]
    public async Task<ActionResult<ApiResponse<object>>> Preview([FromBody] PreviewOrderRequest request)
    {
        var result = await _mediator.Send(new PreviewOrderCommand(
            request.ProductId,
            request.Quantity,
            request.ShippingAddressId,
            request.DeliverySpeed,
            request.PaymentMethod));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateOrderRequest request)
    {
        var result = await _mediator.Send(new CreateOrderCommand(
            request.ProductId,
            request.Quantity,
            request.ShippingAddressId,
            request.DeliverySpeed,
            request.PaymentMethod,
            request.Note));
        return Ok(ApiResponse<object>.Ok(result, "Order created."));
    }

    [HttpGet("my-purchases")]
    public async Task<ActionResult<ApiResponse<object>>> GetMyPurchases(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] OrderStatus? status = null)
    {
        var result = await _mediator.Send(new GetMyPurchasesQuery(page, pageSize, status));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("my-sales")]
    public async Task<ActionResult<ApiResponse<object>>> GetMySales(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] OrderStatus? status = null)
    {
        var result = await _mediator.Send(new GetMySalesQuery(page, pageSize, status));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetOrderByIdQuery(id));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("{id:guid}/payment-proof")]
    public async Task<ActionResult<ApiResponse<object>>> UploadPaymentProof(Guid id, [FromBody] UploadPaymentProofRequest request)
    {
        var result = await _mediator.Send(new UploadPaymentProofCommand(id, request.ProofImageUrl));
        return Ok(ApiResponse<object>.Ok(result, "Payment proof uploaded."));
    }

    [HttpPost("{id:guid}/confirm-payment")]
    public async Task<ActionResult<ApiResponse<object>>> ConfirmPayment(Guid id, [FromBody] NoteRequest? request)
    {
        var result = await _mediator.Send(new ConfirmPaymentCommand(id, request?.Note));
        return Ok(ApiResponse<object>.Ok(result, "Payment confirmed."));
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<ApiResponse<object>>> Confirm(Guid id, [FromBody] NoteRequest? request)
    {
        var result = await _mediator.Send(new ConfirmOrderCommand(id, request?.Note));
        return Ok(ApiResponse<object>.Ok(result, "Order confirmed."));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<object>>> Reject(Guid id, [FromBody] ReasonRequest request)
    {
        var result = await _mediator.Send(new RejectOrderCommand(id, request.Reason));
        return Ok(ApiResponse<object>.Ok(result, "Order rejected."));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<object>>> Cancel(Guid id, [FromBody] ReasonRequest? request)
    {
        var result = await _mediator.Send(new CancelOrderCommand(id, request?.Reason));
        return Ok(ApiResponse<object>.Ok(result, "Order cancelled."));
    }

    [HttpPost("{id:guid}/mark-prepared")]
    public async Task<ActionResult<ApiResponse<object>>> MarkPrepared(Guid id)
    {
        var result = await _mediator.Send(new MarkPreparedCommand(id));
        return Ok(ApiResponse<object>.Ok(result, "Order marked as prepared."));
    }

    [HttpPost("{id:guid}/hand-over")]
    public async Task<ActionResult<ApiResponse<object>>> HandOver(Guid id, [FromBody] HandOverRequest request)
    {
        var result = await _mediator.Send(new HandOverToCourierCommand(
            id,
            request.DeliveryPersonName,
            request.DeliveryPersonPhone,
            request.DeliveryCompany,
            request.VehicleNumber,
            request.TrackingCode,
            request.DeliveryNote,
            request.EstimatedDeliveryFrom,
            request.EstimatedDeliveryTo));
        return Ok(ApiResponse<object>.Ok(result, "Handed over to courier."));
    }

    [HttpPost("{id:guid}/confirm-delivered")]
    public async Task<ActionResult<ApiResponse<object>>> ConfirmDelivered(Guid id)
    {
        var result = await _mediator.Send(new ConfirmDeliveredCommand(id));
        return Ok(ApiResponse<object>.Ok(result, "Delivery confirmed."));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<ApiResponse<object>>> Complete(Guid id)
    {
        var result = await _mediator.Send(new CompleteOrderCommand(id));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("{id:guid}/fail-delivery")]
    public async Task<ActionResult<ApiResponse<object>>> FailDelivery(Guid id, [FromBody] ReasonRequest request)
    {
        var result = await _mediator.Send(new FailDeliveryCommand(id, request.Reason));
        return Ok(ApiResponse<object>.Ok(result, "Delivery marked as failed."));
    }
}
