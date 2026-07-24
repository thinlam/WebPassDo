using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PassDo.Api.Contracts.Categories;
using PassDo.Application.Categories.Commands.CreateCategory;
using PassDo.Application.Categories.Commands.DeleteCategory;
using PassDo.Application.Categories.Commands.UpdateCategory;
using PassDo.Application.Categories.Queries.GetCategories;
using PassDo.Application.Categories.Queries.GetCategoryById;
using PassDo.Application.Common.Models;
using PassDo.Domain.Constants;

namespace PassDo.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(includeInactive));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetCategoryByIdQuery(id));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> Create([FromBody] CreateCategoryRequest request)
    {
        var result = await _mediator.Send(new CreateCategoryCommand(
            request.Name,
            request.Description,
            request.Slug,
            request.DisplayOrder,
            request.IsActive));

        return Ok(ApiResponse<object>.Ok(result, "Category created."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        var result = await _mediator.Send(new UpdateCategoryCommand(
            id,
            request.Name,
            request.Description,
            request.Slug,
            request.DisplayOrder,
            request.IsActive));

        return Ok(ApiResponse<object>.Ok(result, "Category updated."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        await _mediator.Send(new DeleteCategoryCommand(id));
        return Ok(ApiResponse<object>.Ok(null!, "Category deleted."));
    }
}
