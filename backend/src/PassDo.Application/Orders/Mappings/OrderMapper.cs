using PassDo.Application.Orders.DTOs;
using PassDo.Domain.Entities;

namespace PassDo.Application.Orders.Mappings;

public static class OrderMapper
{
    public static string MaskAccountNumber(string? accountNumber)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            return string.Empty;
        }

        var digits = accountNumber.Trim();
        if (digits.Length <= 4)
        {
            return new string('*', digits.Length);
        }

        return $"**** **** {digits[^4..]}";
    }

    public static string FormatAddress(string street, string ward, string district, string province)
        => string.Join(", ", new[] { street, ward, district, province }.Where(x => !string.IsNullOrWhiteSpace(x)));

    public static OrderListItemDto ToListItemDto(Order order)
    {
        var item = order.Items.FirstOrDefault();
        return new OrderListItemDto
        {
            Id = order.Id,
            OrderCode = order.OrderCode,
            ProductId = order.ProductId,
            ProductName = item?.ProductName ?? order.Product?.Name ?? string.Empty,
            ProductImageUrl = item?.ProductImageUrl
                ?? order.Product?.Images
                    .OrderByDescending(x => x.IsPrimary)
                    .ThenBy(x => x.DisplayOrder)
                    .Select(x => x.Url)
                    .FirstOrDefault(),
            Quantity = item?.Quantity ?? 1,
            ProductTotal = order.ProductTotal,
            ShippingFee = order.ShippingFee,
            GrandTotal = order.GrandTotal,
            Status = order.Status.ToString(),
            PaymentMethod = order.PaymentMethod.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            DeliverySpeed = order.DeliverySpeed.ToString(),
            EstimatedDeliveryFrom = order.EstimatedDeliveryFrom,
            EstimatedDeliveryTo = order.EstimatedDeliveryTo,
            CreatedAt = order.CreatedAt,
            BuyerId = order.BuyerId,
            BuyerName = order.Buyer?.FullName,
            SellerId = order.SellerId,
            SellerName = order.Seller?.FullName,
            ShipperId = order.ShipperId,
            ShipperName = order.Shipper?.FullName
        };
    }

    public static OrderDetailDto ToDetailDto(Order order, bool includeSensitiveContact, bool includeFullBankAccount)
    {
        var dto = new OrderDetailDto
        {
            Id = order.Id,
            OrderCode = order.OrderCode,
            ProductId = order.ProductId,
            ProductName = order.Items.FirstOrDefault()?.ProductName ?? order.Product?.Name ?? string.Empty,
            ProductImageUrl = order.Items.FirstOrDefault()?.ProductImageUrl,
            Quantity = order.Items.FirstOrDefault()?.Quantity ?? 1,
            ProductTotal = order.ProductTotal,
            ShippingFee = order.ShippingFee,
            GrandTotal = order.GrandTotal,
            Status = order.Status.ToString(),
            PaymentMethod = order.PaymentMethod.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            DeliverySpeed = order.DeliverySpeed.ToString(),
            EstimatedDeliveryFrom = order.EstimatedDeliveryFrom,
            EstimatedDeliveryTo = order.EstimatedDeliveryTo,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            BuyerId = order.BuyerId,
            BuyerName = order.Buyer?.FullName,
            SellerId = order.SellerId,
            SellerName = order.Seller?.FullName,
            ShipperId = order.ShipperId,
            ShipperName = order.Shipper?.FullName,
            Note = order.Note,
            CancellationReason = order.CancellationReason,
            ConfirmedAt = order.ConfirmedAt,
            PreparedAt = order.PreparedAt,
            PickedUpAt = order.PickedUpAt,
            DeliveredAt = order.DeliveredAt,
            CancelledAt = order.CancelledAt,
            Items = order.Items.Select(x => new OrderItemDto
            {
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                ProductImageUrl = x.ProductImageUrl,
                UnitPrice = x.UnitPrice,
                Quantity = x.Quantity,
                LineTotal = x.LineTotal
            }).ToList(),
            StatusHistory = order.StatusHistories
                .OrderBy(x => x.CreatedAt)
                .Select(x => new OrderStatusHistoryDto
                {
                    OldStatus = x.OldStatus?.ToString(),
                    NewStatus = x.NewStatus.ToString(),
                    ChangedByRole = x.ChangedByRole,
                    ChangedByName = x.ChangedByUser?.FullName,
                    Note = x.Note,
                    CreatedAt = x.CreatedAt
                }).ToList()
        };

        if (includeSensitiveContact)
        {
            dto.Seller = order.Seller is null ? null : new OrderPartyDto
            {
                Id = order.Seller.Id,
                FullName = order.Seller.FullName,
                PhoneNumber = order.Seller.PhoneNumber ?? order.PickupPhone
            };
            dto.Buyer = order.Buyer is null ? null : new OrderPartyDto
            {
                Id = order.Buyer.Id,
                FullName = order.Buyer.FullName,
                PhoneNumber = order.Buyer.PhoneNumber ?? order.ShippingPhone
            };
            dto.Shipper = order.Shipper is null ? null : new OrderPartyDto
            {
                Id = order.Shipper.Id,
                FullName = order.Shipper.FullName,
                PhoneNumber = order.Shipper.PhoneNumber
            };
            dto.ShippingAddress = new OrderAddressDto
            {
                RecipientName = order.ShippingRecipientName,
                PhoneNumber = order.ShippingPhone,
                Province = order.ShippingProvince,
                District = order.ShippingDistrict,
                Ward = order.ShippingWard,
                StreetAddress = order.ShippingStreetAddress,
                Note = order.ShippingAddressNote,
                FullAddress = FormatAddress(order.ShippingStreetAddress, order.ShippingWard, order.ShippingDistrict, order.ShippingProvince)
            };
            dto.PickupAddress = new OrderAddressDto
            {
                RecipientName = order.PickupRecipientName,
                PhoneNumber = order.PickupPhone,
                Province = order.PickupProvince,
                District = order.PickupDistrict,
                Ward = order.PickupWard,
                StreetAddress = order.PickupStreetAddress,
                FullAddress = FormatAddress(order.PickupStreetAddress, order.PickupWard, order.PickupDistrict, order.PickupProvince)
            };
        }
        else
        {
            dto.Seller = order.Seller is null ? null : new OrderPartyDto { Id = order.Seller.Id, FullName = order.Seller.FullName };
            dto.Buyer = order.Buyer is null ? null : new OrderPartyDto { Id = order.Buyer.Id, FullName = order.Buyer.FullName };
            dto.Shipper = order.Shipper is null ? null : new OrderPartyDto { Id = order.Shipper.Id, FullName = order.Shipper.FullName };
        }

        if (order.Payment is not null)
        {
            dto.Payment = new OrderPaymentDto
            {
                Method = order.Payment.Method.ToString(),
                Status = order.Payment.Status.ToString(),
                Amount = order.Payment.Amount,
                TransferContent = order.Payment.TransferContent,
                ProofImageUrl = order.Payment.ProofImageUrl,
                ConfirmedAt = order.Payment.ConfirmedAt
            };
        }

        if (order.Shipment is not null)
        {
            dto.Shipment = new OrderShipmentDto
            {
                CarrierName = order.Shipment.CarrierName,
                TrackingCode = order.Shipment.TrackingCode,
                DeliverySpeed = order.Shipment.DeliverySpeed.ToString(),
                SenderCity = order.Shipment.SenderCity,
                ReceiverCity = order.Shipment.ReceiverCity,
                ShippingFee = order.Shipment.ShippingFee,
                EstimatedDeliveryFrom = order.Shipment.EstimatedDeliveryFrom,
                EstimatedDeliveryTo = order.Shipment.EstimatedDeliveryTo,
                SellerHandedOverAt = order.Shipment.SellerHandedOverAt,
                ShipperReceivedAt = order.Shipment.ShipperReceivedAt,
                DeliveredAt = order.Shipment.DeliveredAt,
                DeliveryNote = order.Shipment.DeliveryNote,
                ShipperId = order.Shipment.ShipperId,
                ShipperName = order.Shipper?.FullName,
                ShipperPhone = includeSensitiveContact ? order.Shipper?.PhoneNumber : null
            };
        }

        if (!string.IsNullOrWhiteSpace(order.BankAccountNumberSnapshot))
        {
            dto.SellerBankAccount = new OrderBankSnapshotDto
            {
                BankName = order.BankNameSnapshot ?? string.Empty,
                AccountHolderName = order.BankAccountHolderSnapshot ?? string.Empty,
                Branch = order.BankBranchSnapshot,
                AccountNumber = includeFullBankAccount ? order.BankAccountNumberSnapshot! : MaskAccountNumber(order.BankAccountNumberSnapshot),
                AccountNumberMasked = MaskAccountNumber(order.BankAccountNumberSnapshot)
            };
        }

        return dto;
    }
}
