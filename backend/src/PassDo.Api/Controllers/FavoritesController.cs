using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PassDo.Application.Common.Models;
using PassDo.Application.Favorites.Commands.AddFavorite;
using PassDo.Application.Favorites.Commands.RemoveFavorite;
using PassDo.Application.Favorites.Queries.GetFavorites;

namespace PassDo.Api.Controllers;

[ApiController]
[Route("api/favorites")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FavoritesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<object>>> GetFavorites(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _mediator.Send(new GetFavoritesQuery(page, pageSize));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPost("{productId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Add(Guid productId)
    {
        var result = await _mediator.Send(new AddFavoriteCommand(productId));
        return Ok(ApiResponse<object>.Ok(result, "Added to favorites."));
    }

    [HttpDelete("{productId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Remove(Guid productId)
    {
        await _mediator.Send(new RemoveFavoriteCommand(productId));
        return Ok(ApiResponse<object>.Ok(null!, "Removed from favorites."));
    }
}
