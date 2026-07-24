using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PassDo.Api.Contracts.Products;
using PassDo.Application.Common.Models;
using PassDo.Application.Products.Commands.CreateProduct;
using PassDo.Application.Products.Commands.DeleteProduct;
using PassDo.Application.Products.Commands.DeleteProductImage;
using PassDo.Application.Products.Commands.SetPrimaryProductImage;
using PassDo.Application.Products.Commands.UpdateProduct;
using PassDo.Application.Products.Commands.UpdateProductStatus;
using PassDo.Application.Products.Commands.UploadProductImage;
using PassDo.Application.Products.Queries.GetMyProducts;
using PassDo.Application.Products.Queries.GetProductById;
using PassDo.Application.Products.Queries.GetProducts;
using PassDo.Domain.Enums;

namespace PassDo.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? keyword = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] ProductCondition? condition = null,
        [FromQuery] ProductStatus? status = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? location = null,
        [FromQuery] string? sortBy = "createdAt",
        [FromQuery] string? sortDirection = "desc")
    {
        var result = await _mediator.Send(new GetProductsQuery(
            page,
            pageSize,
            keyword,
            categoryId,
            condition,
            status,
            minPrice,
            maxPrice,
            location,
            sortBy,
            sortDirection));

        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("my-products")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> GetMyProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] ProductStatus? status = null)
    {
        var result = await _mediator.Send(new GetMyProductsQuery(page, pageSize, status));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateProductRequest request)
    {
        var speeds = request.AllowedDeliverySpeeds is { Count: > 0 }
            ? request.AllowedDeliverySpeeds
            : new[] { DeliverySpeed.Standard, DeliverySpeed.Intercity };

        var result = await _mediator.Send(new CreateProductCommand(
            request.Name,
            request.Description,
            request.OriginalPrice,
            request.SellingPrice,
            request.Condition,
            request.CategoryId,
            request.Location,
            request.Quantity,
            request.PickupAddressId,
            request.BankAccountId,
            request.AcceptedPaymentOption,
            speeds,
            request.Status));

        return Ok(ApiResponse<object>.Ok(result, "Product created."));
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] UpdateProductRequest request)
    {
        var result = await _mediator.Send(new UpdateProductCommand(
            id,
            request.Name,
            request.Description,
            request.OriginalPrice,
            request.SellingPrice,
            request.Condition,
            request.CategoryId,
            request.Location,
            request.Quantity,
            request.PickupAddressId,
            request.BankAccountId,
            request.AcceptedPaymentOption,
            request.AllowedDeliverySpeeds,
            request.Status));

        return Ok(ApiResponse<object>.Ok(result, "Product updated."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        await _mediator.Send(new DeleteProductCommand(id));
        return Ok(ApiResponse<object>.Ok(null!, "Product deleted."));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(
        Guid id,
        [FromBody] UpdateProductStatusRequest request)
    {
        var result = await _mediator.Send(new UpdateProductStatusCommand(id, request.Status));
        return Ok(ApiResponse<object>.Ok(result, "Product status updated."));
    }

    [HttpPost("{productId:guid}/images")]
    [Authorize]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<object>>> UploadImage(
        Guid productId,
        IFormFile file,
        [FromForm] bool setAsPrimary = false)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<object>.Fail("Image file is required."));
        }

        await using var stream = file.OpenReadStream();
        var result = await _mediator.Send(new UploadProductImageCommand(
            productId,
            stream,
            file.FileName,
            file.ContentType,
            setAsPrimary));

        return Ok(ApiResponse<object>.Ok(result, "Image uploaded."));
    }

    [HttpDelete("{productId:guid}/images/{imageId:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> DeleteImage(Guid productId, Guid imageId)
    {
        await _mediator.Send(new DeleteProductImageCommand(productId, imageId));
        return Ok(ApiResponse<object>.Ok(null!, "Image deleted."));
    }

    [HttpPatch("{productId:guid}/images/{imageId:guid}/primary")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> SetPrimaryImage(Guid productId, Guid imageId)
    {
        var result = await _mediator.Send(new SetPrimaryProductImageCommand(productId, imageId));
        return Ok(ApiResponse<object>.Ok(result, "Primary image updated."));
    }
}
