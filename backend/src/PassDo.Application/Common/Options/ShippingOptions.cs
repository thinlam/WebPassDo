using PassDo.Domain.Enums;

namespace PassDo.Application.Common.Options;

public class ShippingOptions
{
    public const string SectionName = "Shipping";

    public ShippingEtaOptions Eta { get; set; } = new();
    public ShippingFeeOptions Fees { get; set; } = new();
}

public class ShippingEtaOptions
{
    public int ExpressMinutesMin { get; set; } = 30;
    public int ExpressMinutesMax { get; set; } = 120;
    public bool SameDayEndOfDay { get; set; } = true;
    public int StandardDaysMin { get; set; } = 1;
    public int StandardDaysMax { get; set; } = 2;
    public int IntercityDaysMin { get; set; } = 2;
    public int IntercityDaysMax { get; set; } = 5;
}

public class ShippingFeeOptions
{
    public decimal Express { get; set; } = 45000;
    public decimal SameDay { get; set; } = 35000;
    public decimal Standard { get; set; } = 25000;
    public decimal Intercity { get; set; } = 40000;
}

public interface IShippingCalculator
{
    decimal GetShippingFee(DeliverySpeed speed);
    (DateTime From, DateTime To) CalculateEta(DeliverySpeed speed, DateTime handedOverAtUtc);
}
