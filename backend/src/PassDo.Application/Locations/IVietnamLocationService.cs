using PassDo.Application.Locations.DTOs;

namespace PassDo.Application.Locations;

public interface IVietnamLocationService
{
    Task<IReadOnlyList<LocationItemDto>> GetProvincesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<LocationItemDto>> GetDistrictsAsync(string provinceCode, CancellationToken cancellationToken);
    Task<IReadOnlyList<LocationItemDto>> GetWardsAsync(string districtCode, CancellationToken cancellationToken);
}
