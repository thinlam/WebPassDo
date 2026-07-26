using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PassDo.Application.Common.Models;
using PassDo.Application.Locations.Queries;

namespace PassDo.Api.Controllers;

[ApiController]
[Route("api/locations")]
[AllowAnonymous]
public class LocationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LocationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("provinces")]
    public async Task<ActionResult<ApiResponse<object>>> GetProvinces()
    {
        var result = await _mediator.Send(new GetProvincesQuery());
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("districts")]
    public async Task<ActionResult<ApiResponse<object>>> GetDistricts([FromQuery] string provinceCode)
    {
        var result = await _mediator.Send(new GetDistrictsQuery(provinceCode));
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("wards")]
    public async Task<ActionResult<ApiResponse<object>>> GetWards([FromQuery] string districtCode)
    {
        var result = await _mediator.Send(new GetWardsQuery(districtCode));
        return Ok(ApiResponse<object>.Ok(result));
    }
}
