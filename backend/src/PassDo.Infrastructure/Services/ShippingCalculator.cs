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
}
