using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PassDo.Api.Contracts.Settings;
using PassDo.Application.Addresses.Commands;
using PassDo.Application.BankAccounts.Commands;
using PassDo.Application.Common.Models;
using PassDo.Domain.Enums;

namespace PassDo.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeSettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MeSettingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("addresses")]
    public async Task<ActionResult<ApiResponse<object>>> GetAddresses()
        => Ok(ApiResponse<object>.Ok(await _mediator.Send(new GetMyAddressesQuery())));

    [HttpPost("addresses")]
    public async Task<ActionResult<ApiResponse<object>>> CreateAddress([FromBody] UpsertAddressRequest request)
    {
        var result = await _mediator.Send(new CreateAddressCommand(
            request.RecipientName,
            request.PhoneNumber,
            request.Province,
            request.District,
            request.Ward,
            request.StreetAddress,
            request.Note,
            request.AddressType,
            request.IsDefault,
            request.ProvinceCode,
            request.DistrictCode,
            request.WardCode));
        return Ok(ApiResponse<object>.Ok(result, "Address created."));
    }

    [HttpPut("addresses/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateAddress(Guid id, [FromBody] UpsertAddressRequest request)
    {
        var result = await _mediator.Send(new UpdateAddressCommand(
            id,
            request.RecipientName,
            request.PhoneNumber,
            request.Province,
            request.District,
            request.Ward,
            request.StreetAddress,
            request.Note,
            request.AddressType,
            request.IsDefault,
            request.ProvinceCode,
            request.DistrictCode,
            request.WardCode));
        return Ok(ApiResponse<object>.Ok(result, "Address updated."));
    }

    [HttpDelete("addresses/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAddress(Guid id)
    {
        await _mediator.Send(new DeleteAddressCommand(id));
        return Ok(ApiResponse<object>.Ok(new { }, "Address deleted."));
    }

    [HttpPut("addresses/{id:guid}/default")]
    public async Task<ActionResult<ApiResponse<object>>> SetDefaultAddress(Guid id)
        => Ok(ApiResponse<object>.Ok(await _mediator.Send(new SetDefaultAddressCommand(id)), "Default address set."));

    [HttpGet("bank-accounts")]
    public async Task<ActionResult<ApiResponse<object>>> GetBankAccounts()
        => Ok(ApiResponse<object>.Ok(await _mediator.Send(new GetMyBankAccountsQuery())));

    [HttpPost("bank-accounts")]
    public async Task<ActionResult<ApiResponse<object>>> CreateBankAccount([FromBody] UpsertBankAccountRequest request)
    {
        var result = await _mediator.Send(new CreateBankAccountCommand(
            request.BankName,
            request.AccountNumber,
            request.AccountHolderName,
            request.Branch,
            request.IsDefault));
        return Ok(ApiResponse<object>.Ok(result, "Bank account created."));
    }

    [HttpPut("bank-accounts/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateBankAccount(Guid id, [FromBody] UpsertBankAccountRequest request)
    {
        var result = await _mediator.Send(new UpdateBankAccountCommand(
            id,
            request.BankName,
            request.AccountNumber,
            request.AccountHolderName,
            request.Branch,
            request.IsDefault));
        return Ok(ApiResponse<object>.Ok(result, "Bank account updated."));
    }

    [HttpDelete("bank-accounts/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteBankAccount(Guid id)
    {
        await _mediator.Send(new DeleteBankAccountCommand(id));
        return Ok(ApiResponse<object>.Ok(new { }, "Bank account deleted."));
    }

    [HttpPut("bank-accounts/{id:guid}/default")]
    public async Task<ActionResult<ApiResponse<object>>> SetDefaultBankAccount(Guid id)
        => Ok(ApiResponse<object>.Ok(await _mediator.Send(new SetDefaultBankAccountCommand(id)), "Default bank account set."));
}
