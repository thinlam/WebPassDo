using PassDo.Domain.Enums;

namespace PassDo.Application.Common.Options;

public class ShippingOptions
{
    public const string SectionName = "Shipping";

    public ShippingEtaOptions Eta { get; set; } = new();
    public ShippingFeeOptions Fees { get; set; } = new();
    public InnerCityOptions InnerCity { get; set; } = new();
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
    public decimal SameProvinceOuter { get; set; } = 25000;
    public decimal NearbyProvince { get; set; } = 40000;
    public decimal FarProvince { get; set; } = 55000;
}

public class InnerCityOptions
{
    /// <summary>Provinces where same-province + listed districts = free shipping.</summary>
    public List<string> Provinces { get; set; } = ["TP.HCM", "Hồ Chí Minh", "Ha Noi", "Hà Nội"];
    public List<string> Districts { get; set; } =
    [
        "Quận 1", "Quan 1", "Quận 3", "Quan 3", "Quận 4", "Quan 4",
        "Quận 5", "Quan 5", "Quận 10", "Quan 10", "Bình Thạnh", "Binh Thanh",
        "Phú Nhuận", "Phu Nhuan", "Tân Bình", "Tan Binh",
        "Ba Đình", "Ba Dinh", "Hoàn Kiếm", "Hoan Kiem", "Đống Đa", "Dong Da", "Cầu Giấy", "Cau Giay"
    ];
    public List<string> NearbyProvincePairs { get; set; } =
    [
        "TP.HCM|Đồng Nai", "TP.HCM|Bình Dương", "Hồ Chí Minh|Đồng Nai", "Hồ Chí Minh|Bình Dương",
        "Hà Nội|Hưng Yên", "Ha Noi|Hung Yen", "Hà Nội|Bắc Ninh", "Ha Noi|Bac Ninh"
    ];
}

public class ShippingQuote
{
    public bool IsInnerCity { get; set; }
    public decimal? DistanceKm { get; set; }
    public decimal ShippingFee { get; set; }
    public DateTime EstimatedDeliveryFrom { get; set; }
    public DateTime EstimatedDeliveryTo { get; set; }
    public string Description { get; set; } = string.Empty;
    public DeliverySpeed SuggestedSpeed { get; set; }
}

public interface IShippingCalculator
{
    decimal GetShippingFee(DeliverySpeed speed);
    (DateTime From, DateTime To) CalculateEta(DeliverySpeed speed, DateTime handedOverAtUtc);
    ShippingQuote CalculateForAddresses(
        string pickupProvince,
        string pickupDistrict,
        string deliveryProvince,
        string deliveryDistrict,
        DeliverySpeed? preferredSpeed,
        DateTime nowUtc);
}
