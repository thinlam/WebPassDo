using FluentValidation;
using MediatR;
using PassDo.Application.Locations.DTOs;

namespace PassDo.Application.Locations.Queries;

public record GetProvincesQuery() : IRequest<IReadOnlyList<LocationItemDto>>;
public record GetDistrictsQuery(string ProvinceCode) : IRequest<IReadOnlyList<LocationItemDto>>;
public record GetWardsQuery(string DistrictCode) : IRequest<IReadOnlyList<LocationItemDto>>;

public class GetDistrictsQueryValidator : AbstractValidator<GetDistrictsQuery>
{
    public GetDistrictsQueryValidator() => RuleFor(x => x.ProvinceCode).NotEmpty();
}

public class GetWardsQueryValidator : AbstractValidator<GetWardsQuery>
{
    public GetWardsQueryValidator() => RuleFor(x => x.DistrictCode).NotEmpty();
}

public class LocationQueryHandlers :
    IRequestHandler<GetProvincesQuery, IReadOnlyList<LocationItemDto>>,
    IRequestHandler<GetDistrictsQuery, IReadOnlyList<LocationItemDto>>,
    IRequestHandler<GetWardsQuery, IReadOnlyList<LocationItemDto>>
{
    private readonly IVietnamLocationService _locationService;

    public LocationQueryHandlers(IVietnamLocationService locationService)
    {
        _locationService = locationService;
    }

    public Task<IReadOnlyList<LocationItemDto>> Handle(GetProvincesQuery request, CancellationToken cancellationToken)
        => _locationService.GetProvincesAsync(cancellationToken);

    public Task<IReadOnlyList<LocationItemDto>> Handle(GetDistrictsQuery request, CancellationToken cancellationToken)
        => _locationService.GetDistrictsAsync(request.ProvinceCode, cancellationToken);

    public Task<IReadOnlyList<LocationItemDto>> Handle(GetWardsQuery request, CancellationToken cancellationToken)
        => _locationService.GetWardsAsync(request.DistrictCode, cancellationToken);
}
