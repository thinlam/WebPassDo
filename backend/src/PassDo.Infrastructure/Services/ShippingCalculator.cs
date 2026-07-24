using Microsoft.Extensions.Options;
using PassDo.Application.Common.Options;
using PassDo.Domain.Enums;

namespace PassDo.Infrastructure.Services;

public class ShippingCalculator : IShippingCalculator
{
    private readonly ShippingOptions _options;

    public ShippingCalculator(IOptions<ShippingOptions> options)
    {
        _options = options.Value;
    }

    public decimal GetShippingFee(DeliverySpeed speed) => speed switch
    {
        DeliverySpeed.Express => _options.Fees.Express,
        DeliverySpeed.SameDay => _options.Fees.SameDay,
        DeliverySpeed.Standard => _options.Fees.Standard,
        DeliverySpeed.Intercity => _options.Fees.Intercity,
        _ => _options.Fees.Standard
    };

    public (DateTime From, DateTime To) CalculateEta(DeliverySpeed speed, DateTime handedOverAtUtc)
    {
        var eta = _options.Eta;
        return speed switch
        {
            DeliverySpeed.Express => (
                handedOverAtUtc.AddMinutes(eta.ExpressMinutesMin),
                handedOverAtUtc.AddMinutes(eta.ExpressMinutesMax)),
            DeliverySpeed.SameDay => (
                handedOverAtUtc,
                eta.SameDayEndOfDay
                    ? handedOverAtUtc.Date.AddDays(1).AddTicks(-1)
                    : handedOverAtUtc.AddHours(12)),
            DeliverySpeed.Standard => (
                handedOverAtUtc.AddDays(eta.StandardDaysMin),
                handedOverAtUtc.AddDays(eta.StandardDaysMax)),
            DeliverySpeed.Intercity => (
                handedOverAtUtc.AddDays(eta.IntercityDaysMin),
                handedOverAtUtc.AddDays(eta.IntercityDaysMax)),
            _ => (
                handedOverAtUtc.AddDays(eta.StandardDaysMin),
                handedOverAtUtc.AddDays(eta.StandardDaysMax))
        };
    }

    public ShippingQuote CalculateForAddresses(
        string pickupProvince,
        string pickupDistrict,
        string deliveryProvince,
        string deliveryDistrict,
        DeliverySpeed? preferredSpeed,
        DateTime nowUtc)
    {
        var sameProvince = Normalize(pickupProvince) == Normalize(deliveryProvince);
        var pickupInner = IsInnerDistrict(pickupProvince, pickupDistrict);
        var deliveryInner = IsInnerDistrict(deliveryProvince, deliveryDistrict);
        var isInnerCity = sameProvince && pickupInner && deliveryInner;

        DeliverySpeed speed;
        decimal fee;
        string description;

        if (isInnerCity)
        {
            speed = preferredSpeed is DeliverySpeed.Express or DeliverySpeed.SameDay
                ? preferredSpeed.Value
                : DeliverySpeed.SameDay;
            fee = 0;
            description = "Miễn phí giao hàng nội thành";
        }
        else if (sameProvince)
        {
            speed = preferredSpeed ?? DeliverySpeed.Standard;
            fee = _options.Fees.SameProvinceOuter;
            description = "Phí giao cùng tỉnh (ngoài nội thành)";
        }
        else if (IsNearbyProvince(pickupProvince, deliveryProvince))
        {
            speed = preferredSpeed ?? DeliverySpeed.Intercity;
            fee = _options.Fees.NearbyProvince;
            description = "Phí giao tỉnh lân cận";
        }
        else
        {
            speed = preferredSpeed ?? DeliverySpeed.Intercity;
            fee = _options.Fees.FarProvince;
            description = "Phí giao khác khu vực";
        }

        // Prefer configured speed-based fee if caller asked for express and not free inner-city
        if (!isInnerCity && preferredSpeed.HasValue)
        {
            var bySpeed = GetShippingFee(preferredSpeed.Value);
            if (bySpeed > fee)
            {
                fee = bySpeed;
                speed = preferredSpeed.Value;
            }
        }

        var eta = CalculateEta(speed, nowUtc);
        return new ShippingQuote
        {
            IsInnerCity = isInnerCity,
            DistanceKm = null,
            ShippingFee = fee,
            EstimatedDeliveryFrom = eta.From,
            EstimatedDeliveryTo = eta.To,
            Description = description,
            SuggestedSpeed = speed
        };
    }

    private bool IsInnerDistrict(string province, string district)
    {
        var p = Normalize(province);
        var d = Normalize(district);
        var provinceOk = _options.InnerCity.Provinces.Any(x => Normalize(x) == p);
        if (!provinceOk)
        {
            return false;
        }

        return _options.InnerCity.Districts.Any(x => Normalize(x) == d);
    }

    private bool IsNearbyProvince(string a, string b)
    {
        var left = Normalize(a);
        var right = Normalize(b);
        return _options.InnerCity.NearbyProvincePairs.Any(pair =>
        {
            var parts = pair.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                return false;
            }

            var x = Normalize(parts[0]);
            var y = Normalize(parts[1]);
            return (left == x && right == y) || (left == y && right == x);
        });
    }

    private static string Normalize(string? value)
        => (value ?? string.Empty)
            .Trim()
            .ToLowerInvariant()
            .Replace("thành phố ", string.Empty)
            .Replace("tp.", string.Empty)
            .Replace("tp ", string.Empty)
            .Replace(" ", string.Empty);
}
