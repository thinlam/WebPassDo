using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using PassDo.Application.Locations;
using PassDo.Application.Locations.DTOs;

namespace PassDo.Infrastructure.Services;

/// <summary>
/// Loads Vietnam administrative divisions (province/district/ward) from the public
/// provinces.open-api.vn API and caches the full dataset in memory for 24h so that
/// province/district/ward lookups do not hit the network on every request.
/// </summary>
public class VietnamLocationService : IVietnamLocationService
{
    private const string CacheKey = "vietnam-locations:v1";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<VietnamLocationService> _logger;

    public VietnamLocationService(HttpClient httpClient, IMemoryCache cache, ILogger<VietnamLocationService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LocationItemDto>> GetProvincesAsync(CancellationToken cancellationToken)
    {
        var data = await GetDataAsync(cancellationToken);
        return data.Provinces;
    }

    public async Task<IReadOnlyList<LocationItemDto>> GetDistrictsAsync(string provinceCode, CancellationToken cancellationToken)
    {
        var data = await GetDataAsync(cancellationToken);
        return data.DistrictsByProvinceCode.TryGetValue(provinceCode, out var districts)
            ? districts
            : Array.Empty<LocationItemDto>();
    }

    public async Task<IReadOnlyList<LocationItemDto>> GetWardsAsync(string districtCode, CancellationToken cancellationToken)
    {
        var data = await GetDataAsync(cancellationToken);
        return data.WardsByDistrictCode.TryGetValue(districtCode, out var wards)
            ? wards
            : Array.Empty<LocationItemDto>();
    }

    private Task<LocationData> GetDataAsync(CancellationToken cancellationToken)
    {
        return _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await FetchAsync(cancellationToken);
        })!;
    }

    private async Task<LocationData> FetchAsync(CancellationToken cancellationToken)
    {
        try
        {
            var raw = await _httpClient.GetFromJsonAsync<List<RawProvince>>("?depth=3", cancellationToken)
                ?? new List<RawProvince>();

            var provinces = new List<LocationItemDto>(raw.Count);
            var districtsByProvinceCode = new Dictionary<string, List<LocationItemDto>>();
            var wardsByDistrictCode = new Dictionary<string, List<LocationItemDto>>();

            foreach (var province in raw)
            {
                var provinceCode = province.Code.ToString();
                provinces.Add(new LocationItemDto { Code = provinceCode, Name = province.Name });

                var districts = new List<LocationItemDto>();
                foreach (var district in province.Districts ?? new List<RawDistrict>())
                {
                    var districtCode = district.Code.ToString();
                    districts.Add(new LocationItemDto { Code = districtCode, Name = district.Name });

                    var wards = (district.Wards ?? new List<RawWard>())
                        .Select(w => new LocationItemDto { Code = w.Code.ToString(), Name = w.Name })
                        .ToList();
                    wardsByDistrictCode[districtCode] = wards;
                }

                districtsByProvinceCode[provinceCode] = districts;
            }

            return new LocationData(provinces, districtsByProvinceCode, wardsByDistrictCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or NotSupportedException)
        {
            _logger.LogError(ex, "Failed to fetch Vietnam administrative divisions from provinces.open-api.vn.");
            return new LocationData(
                new List<LocationItemDto>(),
                new Dictionary<string, List<LocationItemDto>>(),
                new Dictionary<string, List<LocationItemDto>>());
        }
    }

    private sealed record LocationData(
        List<LocationItemDto> Provinces,
        Dictionary<string, List<LocationItemDto>> DistrictsByProvinceCode,
        Dictionary<string, List<LocationItemDto>> WardsByDistrictCode);

    private sealed class RawWard
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class RawDistrict
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("wards")]
        public List<RawWard>? Wards { get; set; }
    }

    private sealed class RawProvince
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("districts")]
        public List<RawDistrict>? Districts { get; set; }
    }
}
