using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using PassDo.Application.Common.Exceptions;
using PassDo.Application.Common.Interfaces;
using PassDo.Application.Common.Options;
using PassDo.Application.Orders.DTOs;
using PassDo.Application.Orders.Helpers;
using PassDo.Application.Orders.Mappings;
using PassDo.Domain.Constants;
using PassDo.Domain.Entities;
using PassDo.Domain.Enums;

namespace PassDo.Application.Orders.Commands.OrderActions;

public record ConfirmPaymentCommand(Guid OrderId, string? Note) : IRequest<OrderDetailDto>;
public record UploadPaymentProofCommand(Guid OrderId, string ProofImageUrl) : IRequest<OrderDetailDto>;
public record ConfirmOrderCommand(Guid OrderId, string? Note) : IRequest<OrderDetailDto>;
public record RejectOrderCommand(Guid OrderId, string Reason) : IRequest<OrderDetailDto>;
public record CancelOrderCommand(Guid OrderId, string? Reason) : IRequest<OrderDetailDto>;
public record MarkPreparedCommand(Guid OrderId) : IRequest<OrderDetailDto>;
public record HandOverToCourierCommand(
    Guid OrderId,
    string DeliveryPersonName,
    string DeliveryPersonPhone,
    string DeliveryCompany,
    string? VehicleNumber,
    string? TrackingCode,
    string? DeliveryNote,
    DateTime? EstimatedDeliveryFrom,
    DateTime? EstimatedDeliveryTo) : IRequest<OrderDetailDto>;
public record ConfirmDeliveredCommand(Guid OrderId) : IRequest<OrderDetailDto>;
public record FailDeliveryCommand(Guid OrderId, string Reason) : IRequest<OrderDetailDto>;

public class RejectOrderCommandValidator : AbstractValidator<RejectOrderCommand>
{
    public RejectOrderCommandValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
}

public class FailDeliveryCommandValidator : AbstractValidator<FailDeliveryCommand>
{
    public FailDeliveryCommandValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
}

public class UploadPaymentProofCommandValidator : AbstractValidator<UploadPaymentProofCommand>
{
    public UploadPaymentProofCommandValidator() => RuleFor(x => x.ProofImageUrl).NotEmpty().MaximumLength(500);
}

public class HandOverToCourierCommandValidator : AbstractValidator<HandOverToCourierCommand>
{
    public HandOverToCourierCommandValidator()
    {
        RuleFor(x => x.DeliveryPersonName)
            .Must(v => !string.IsNullOrWhiteSpace(v))
            .WithMessage("Delivery person name is required.")
            .MaximumLength(200);
        RuleFor(x => x.DeliveryPersonPhone)
            .Must(v => !string.IsNullOrWhiteSpace(v))
            .WithMessage("Delivery person phone is required.")
            .MaximumLength(30);
        RuleFor(x => x.DeliveryCompany)
            .Must(v => !string.IsNullOrWhiteSpace(v))
            .WithMessage("Delivery company is required.")
            .MaximumLength(150);
        RuleFor(x => x.VehicleNumber).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.VehicleNumber));
        RuleFor(x => x.TrackingCode).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.TrackingCode));
        RuleFor(x => x.DeliveryNote).MaximumLength(1000).When(x => !string.IsNullOrWhiteSpace(x.DeliveryNote));
    }
}

public class OrderActionHandler :
    IRequestHandler<ConfirmPaymentCommand, OrderDetailDto>,
    IRequestHandler<UploadPaymentProofCommand, OrderDetailDto>,
    IRequestHandler<ConfirmOrderCommand, OrderDetailDto>,
    IRequestHandler<RejectOrderCommand, OrderDetailDto>,
    IRequestHandler<CancelOrderCommand, OrderDetailDto>,
    IRequestHandler<MarkPreparedCommand, OrderDetailDto>,
    IRequestHandler<HandOverToCourierCommand, OrderDetailDto>,
    IRequestHandler<ConfirmDeliveredCommand, OrderDetailDto>,
    IRequestHandler<FailDeliveryCommand, OrderDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IShippingCalculator _shipping;
    private readonly IDateTimeProvider _clock;
    private readonly INotificationService _notifications;

    public OrderActionHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IShippingCalculator shipping,
        IDateTimeProvider clock,
        INotificationService notifications)
    {
        _context = context;
        _currentUser = currentUser;
        _shipping = shipping;
        _clock = clock;
        _notifications = notifications;
    }

    public Task<OrderDetailDto> Handle(ConfirmPaymentCommand request, CancellationToken ct)
        => Transition(request.OrderId, async order =>
        {
            EnsureSellerOrAdmin(order);
            if (order.Status != OrderStatus.AwaitingPayment && order.PaymentStatus != PaymentStatus.AwaitingConfirmation)
            {
                throw new ConflictException("Order is not awaiting payment confirmation.");
            }

            order.PaymentStatus = PaymentStatus.Paid;
            if (order.Payment is not null)
            {
                order.Payment.Status = PaymentStatus.Paid;
                order.Payment.ConfirmedAt = _clock.UtcNow;
                order.Payment.ConfirmedByUserId = _currentUser.UserId;
                order.Payment.Note = request.Note;
            }

            ChangeStatus(order, OrderStatus.PendingConfirmation, "Đã xác nhận thanh toán chuyển khoản.");
            await Task.CompletedTask;
        }, ct);

    public Task<OrderDetailDto> Handle(UploadPaymentProofCommand request, CancellationToken ct)
        => Transition(request.OrderId, order =>
        {
            EnsureBuyer(order);
            if (order.Status != OrderStatus.AwaitingPayment)
            {
                throw new ConflictException("Payment proof can only be uploaded while awaiting payment.");
            }

            if (order.Payment is null)
            {
                throw new ConflictException("Payment record is missing.");
            }

            order.Payment.ProofImageUrl = request.ProofImageUrl;
            order.Payment.Status = PaymentStatus.AwaitingConfirmation;
            order.PaymentStatus = PaymentStatus.AwaitingConfirmation;
            AppendHistory(order, order.Status, order.Status, "Người mua đã tải minh chứng chuyển khoản.");
            return Task.CompletedTask;
        }, ct);

    public Task<OrderDetailDto> Handle(ConfirmOrderCommand request, CancellationToken ct)
        => Transition(request.OrderId, order =>
        {
            EnsureSellerOrAdmin(order);
            if (order.Status != OrderStatus.PendingConfirmation)
            {
                throw new ConflictException("Only pending confirmation orders can be confirmed.");
            }

            order.ConfirmedAt = _clock.UtcNow;
            ChangeStatus(order, OrderStatus.AwaitingPreparation, request.Note ?? "Đơn hàng đã được xác nhận.");
            return Task.CompletedTask;
        }, ct, afterSave: order => NotifyBuyer(
            order,
            NotificationTypes.OrderConfirmed,
            "Đơn hàng đã được xác nhận",
            $"Người bán đã xác nhận đơn hàng {order.OrderCode} - \"{ProductName(order)}\".",
            ct));

    public Task<OrderDetailDto> Handle(RejectOrderCommand request, CancellationToken ct)
        => Transition(request.OrderId, async order =>
        {
            EnsureSellerOrAdmin(order);
            if (order.Status is not (OrderStatus.PendingConfirmation or OrderStatus.AwaitingPayment))
            {
                throw new ConflictException("Order cannot be rejected in the current status.");
            }

            await RestoreStock(order, ct);
            order.CancelledAt = _clock.UtcNow;
            order.CancellationReason = request.Reason;
            ChangeStatus(order, OrderStatus.Cancelled, request.Reason);
        }, ct, afterSave: order => NotifyBuyer(
            order,
            NotificationTypes.OrderCancelled,
            "Đơn hàng đã bị hủy",
            $"Người bán đã từ chối đơn hàng {order.OrderCode} - \"{ProductName(order)}\". Lý do: {request.Reason}",
            ct));

    public Task<OrderDetailDto> Handle(CancelOrderCommand request, CancellationToken ct)
        => Transition(request.OrderId, async order =>
        {
            EnsureBuyer(order);
            if (order.Status is not (OrderStatus.AwaitingPayment or OrderStatus.PendingConfirmation))
            {
                throw new ConflictException("Only unpaid/unconfirmed orders can be cancelled by buyer.");
            }

            await RestoreStock(order, ct);
            order.CancelledAt = _clock.UtcNow;
            order.CancellationReason = request.Reason;
            ChangeStatus(order, OrderStatus.Cancelled, request.Reason ?? "Người mua đã hủy đơn.");
        }, ct, afterSave: order => NotifySeller(
            order,
            NotificationTypes.OrderCancelled,
            "Đơn hàng đã bị hủy",
            $"Người mua đã hủy đơn hàng {order.OrderCode} - \"{ProductName(order)}\"." +
                (string.IsNullOrWhiteSpace(request.Reason) ? string.Empty : $" Lý do: {request.Reason}"),
            ct));

    public Task<OrderDetailDto> Handle(MarkPreparedCommand request, CancellationToken ct)
        => Transition(request.OrderId, order =>
        {
            EnsureSellerOrAdmin(order);
            if (order.Status != OrderStatus.AwaitingPreparation)
            {
                throw new ConflictException("Order is not awaiting preparation.");
            }

            order.PreparedAt = _clock.UtcNow;
            if (order.Shipment is not null)
            {
                order.Shipment.PreparedByUserId = _currentUser.UserId;
            }

            ChangeStatus(order, OrderStatus.AwaitingHandover, "Người bán đã chuẩn bị hàng.");
            return Task.CompletedTask;
        }, ct, afterSave: order => NotifyBuyer(
            order,
            NotificationTypes.OrderPrepared,
            "Đơn hàng đã được chuẩn bị",
            $"Đơn hàng {order.OrderCode} - \"{ProductName(order)}\" đã được chuẩn bị và sẽ sớm được bàn giao vận chuyển.",
            ct));

    public Task<OrderDetailDto> Handle(HandOverToCourierCommand request, CancellationToken ct)
        => Transition(request.OrderId, order =>
        {
            EnsureSellerOrAdmin(order);
            if (order.Status != OrderStatus.AwaitingHandover)
            {
                throw new ConflictException("Order is not awaiting handover to courier.");
            }

            var now = _clock.UtcNow;

            var deliveryPersonName = request.DeliveryPersonName.Trim();
            var deliveryPersonPhone = request.DeliveryPersonPhone.Trim();
            var deliveryCompany = request.DeliveryCompany.Trim();

            if (order.Shipment is not null)
            {
                order.Shipment.DeliveryPersonName = deliveryPersonName;
                order.Shipment.DeliveryPersonPhone = deliveryPersonPhone;
                order.Shipment.DeliveryCompany = deliveryCompany;
                order.Shipment.VehicleNumber = request.VehicleNumber?.Trim();
                order.Shipment.TrackingCode = request.TrackingCode?.Trim();
                order.Shipment.DeliveryNote = request.DeliveryNote?.Trim();
                order.Shipment.PickedUpAt = now;
                order.Shipment.SellerHandedOverAt = now;
                order.Shipment.HandedOverByUserId = _currentUser.UserId;
                order.Shipment.CarrierName = deliveryCompany;
            }

            order.PickedUpAt = now;

            if (request.EstimatedDeliveryFrom.HasValue && request.EstimatedDeliveryTo.HasValue)
            {
                order.EstimatedDeliveryFrom = request.EstimatedDeliveryFrom.Value;
                order.EstimatedDeliveryTo = request.EstimatedDeliveryTo.Value;
                if (order.Shipment is not null)
                {
                    order.Shipment.EstimatedDeliveryFrom = request.EstimatedDeliveryFrom.Value;
                    order.Shipment.EstimatedDeliveryTo = request.EstimatedDeliveryTo.Value;
                }
            }
            else
            {
                var quote = _shipping.CalculateForAddresses(
                    order.PickupProvince, order.PickupDistrict,
                    order.ShippingProvince, order.ShippingDistrict,
                    order.DeliverySpeed, now);
                order.EstimatedDeliveryFrom = quote.EstimatedDeliveryFrom;
                order.EstimatedDeliveryTo = quote.EstimatedDeliveryTo;
                if (order.Shipment is not null)
                {
                    order.Shipment.EstimatedDeliveryFrom = quote.EstimatedDeliveryFrom;
                    order.Shipment.EstimatedDeliveryTo = quote.EstimatedDeliveryTo;
                }
            }

            ChangeStatus(order, OrderStatus.Shipping, $"Đã bàn giao cho {deliveryCompany} ({deliveryPersonName}).");
            return Task.CompletedTask;
        }, ct, afterSave: order => NotifyBuyer(
            order,
            NotificationTypes.OrderHandedOver,
            "Đơn hàng đang được vận chuyển",
            $"Đơn hàng {order.OrderCode} - \"{ProductName(order)}\" đã được bàn giao cho đơn vị vận chuyển.",
            ct));

    public Task<OrderDetailDto> Handle(ConfirmDeliveredCommand request, CancellationToken ct)
    {
        var actorId = _currentUser.UserId;
        return Transition(request.OrderId, async order =>
        {
            var isBuyer = order.BuyerId == _currentUser.UserId;
            var isSeller = order.SellerId == _currentUser.UserId;
            if (!isBuyer && !isSeller && !IsAdmin())
            {
                throw new ForbiddenException("Only buyer, seller, or admin can confirm delivery.");
            }

            if (order.Status != OrderStatus.Shipping)
            {
                throw new ConflictException("Only shipping orders can be marked delivered.");
            }

            var now = _clock.UtcNow;
            order.DeliveredAt = now;
            if (order.Shipment is not null)
            {
                order.Shipment.DeliveredAt = now;
                order.Shipment.DeliveredConfirmedByUserId = _currentUser.UserId;
            }

            if (order.PaymentMethod == PaymentMethod.CashOnDelivery)
            {
                order.PaymentStatus = PaymentStatus.Paid;
                if (order.Payment is not null)
                {
                    order.Payment.Status = PaymentStatus.Paid;
                    order.Payment.ConfirmedAt = now;
                    order.Payment.ConfirmedByUserId = _currentUser.UserId;
                }
            }

            var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == order.ProductId, ct);
            if (product is not null && product.Quantity <= 0)
            {
                product.Status = ProductStatus.Sold;
            }

            ChangeStatus(order, OrderStatus.Delivered, isBuyer ? "Người mua đã nhận hàng." : "Giao hàng thành công.");
        }, ct, afterSave: async order =>
        {
            // Notify the other party (not the actor who just confirmed).
            var confirmedByBuyer = actorId.HasValue && order.BuyerId == actorId.Value;
            if (confirmedByBuyer)
            {
                await NotifySeller(
                    order,
                    NotificationTypes.OrderDelivered,
                    "Người mua đã xác nhận nhận hàng",
                    $"Người mua đã xác nhận nhận đơn {order.OrderCode} - \"{ProductName(order)}\".",
                    ct);
            }
            else
            {
                await NotifyBuyer(
                    order,
                    NotificationTypes.OrderDelivered,
                    "Đơn hàng đã được giao thành công",
                    $"Đơn hàng {order.OrderCode} - \"{ProductName(order)}\" đã giao thành công. Cảm ơn bạn đã mua hàng trên PassDo.",
                    ct);
            }
        });
    }

    public Task<OrderDetailDto> Handle(FailDeliveryCommand request, CancellationToken ct)
        => Transition(request.OrderId, order =>
        {
            EnsureSellerOrAdmin(order);
            if (order.Status != OrderStatus.Shipping)
            {
                throw new ConflictException("Only shipping orders can fail delivery.");
            }

            order.CancellationReason = request.Reason;
            ChangeStatus(order, OrderStatus.DeliveryFailed, request.Reason);
            return Task.CompletedTask;
        }, ct, afterSave: order => NotifyBuyer(
            order,
            NotificationTypes.OrderCancelled,
            "Giao hàng thất bại",
            $"Đơn hàng {order.OrderCode} - \"{ProductName(order)}\" giao thất bại. {request.Reason}",
            ct));

    private async Task<OrderDetailDto> Transition(
        Guid orderId,
        Func<Order, Task> action,
        CancellationToken ct,
        Func<Order, Task>? afterSave = null)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedException();
        }

        var order = await _context.Orders
            .Include(x => x.Items)
            .Include(x => x.Payment)
            .Include(x => x.Shipment)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct)
            ?? throw new NotFoundException("Order", orderId);

        EnsureShipmentStub(order);
        EnsurePaymentStub(order);

        await action(order);
        await _context.SaveChangesAsync(ct);

        if (afterSave is not null)
        {
            await afterSave(order);
        }

        var loaded = await _context.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.Payment)
            .Include(x => x.Shipment)
            .Include(x => x.StatusHistories).ThenInclude(x => x.ChangedByUser)
            .Include(x => x.Buyer)
            .Include(x => x.Seller)
            .FirstAsync(x => x.Id == orderId, ct);

        return OrderMapper.ToDetailDto(loaded, includeSensitiveContact: true, includeFullBankAccount: IsParticipant(loaded));
    }

    private void AppendHistory(Order order, OrderStatus? oldStatus, OrderStatus newStatus, string? note)
    {
        _context.OrderStatusHistories.Add(OrderHelpers.CreateHistory(
            order.Id,
            oldStatus,
            newStatus,
            _currentUser.UserId,
            _currentUser.Role,
            note));
    }

    private void ChangeStatus(Order order, OrderStatus newStatus, string note)
    {
        var old = order.Status;
        order.Status = newStatus;
        AppendHistory(order, old, newStatus, note);
    }

    private void EnsureShipmentStub(Order order)
    {
        if (order.Shipment is not null)
        {
            return;
        }

        var shipment = new OrderShipment
        {
            OrderId = order.Id,
            DeliverySpeed = order.DeliverySpeed,
            SenderCity = string.IsNullOrWhiteSpace(order.PickupProvince) ? "N/A" : order.PickupProvince,
            ReceiverCity = string.IsNullOrWhiteSpace(order.ShippingProvince) ? "N/A" : order.ShippingProvince,
            ShippingFee = order.ShippingFee,
            CarrierName = "PassDo",
            EstimatedDeliveryFrom = order.EstimatedDeliveryFrom,
            EstimatedDeliveryTo = order.EstimatedDeliveryTo
        };

        _context.OrderShipments.Add(shipment);
        order.Shipment = shipment;
    }

    private void EnsurePaymentStub(Order order)
    {
        if (order.Payment is not null)
        {
            return;
        }

        var payment = new OrderPayment
        {
            OrderId = order.Id,
            Method = order.PaymentMethod,
            Status = order.PaymentStatus,
            Amount = order.GrandTotal > 0 ? order.GrandTotal : order.Price,
            TransferContent = order.PaymentMethod == PaymentMethod.BankTransfer
                ? $"PASSDO {order.OrderCode}"
                : null
        };

        _context.OrderPayments.Add(payment);
        order.Payment = payment;
    }

    private async Task RestoreStock(Order order, CancellationToken ct)
    {
        var qty = await _context.OrderItems
            .Where(x => x.OrderId == order.Id)
            .SumAsync(x => (int?)x.Quantity, ct) ?? 0;

        if (qty <= 0)
        {
            qty = 1;
        }

        var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == order.ProductId, ct);
        if (product is null)
        {
            return;
        }

        product.Quantity += qty;
        if (product.Status is ProductStatus.Reserved or ProductStatus.Sold)
        {
            product.Status = ProductStatus.Available;
        }
    }

    private bool IsAdmin() => string.Equals(_currentUser.Role, Roles.Admin, StringComparison.OrdinalIgnoreCase);

    private void EnsureBuyer(Order order)
    {
        if (order.BuyerId != _currentUser.UserId && !IsAdmin())
        {
            throw new ForbiddenException("Only the buyer can perform this action.");
        }
    }

    private void EnsureSellerOrAdmin(Order order)
    {
        if (order.SellerId != _currentUser.UserId && !IsAdmin())
        {
            throw new ForbiddenException("Only the seller or admin can perform this action.");
        }
    }

    private bool IsParticipant(Order order)
        => order.BuyerId == _currentUser.UserId
           || order.SellerId == _currentUser.UserId
           || IsAdmin();

    private static string ProductName(Order order) => order.Items.FirstOrDefault()?.ProductName ?? "sản phẩm";

    private Task NotifyBuyer(Order order, string type, string title, string content, CancellationToken ct)
        => _notifications.NotifyAsync(order.BuyerId, type, title, content, order.Id, "Order", $"/orders/{order.Id}", ct);

    private Task NotifySeller(Order order, string type, string title, string content, CancellationToken ct)
        => _notifications.NotifyAsync(order.SellerId, type, title, content, order.Id, "Order", $"/orders/{order.Id}", ct);
}
