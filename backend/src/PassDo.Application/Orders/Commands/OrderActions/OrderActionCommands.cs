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
public record ClaimOrderCommand(Guid OrderId) : IRequest<OrderDetailDto>;
public record AssignShipperCommand(Guid OrderId, Guid ShipperId) : IRequest<OrderDetailDto>;
public record ConfirmPickupCommand(Guid OrderId, string? TrackingCode) : IRequest<OrderDetailDto>;
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

public class OrderActionHandler :
    IRequestHandler<ConfirmPaymentCommand, OrderDetailDto>,
    IRequestHandler<UploadPaymentProofCommand, OrderDetailDto>,
    IRequestHandler<ConfirmOrderCommand, OrderDetailDto>,
    IRequestHandler<RejectOrderCommand, OrderDetailDto>,
    IRequestHandler<CancelOrderCommand, OrderDetailDto>,
    IRequestHandler<MarkPreparedCommand, OrderDetailDto>,
    IRequestHandler<ClaimOrderCommand, OrderDetailDto>,
    IRequestHandler<AssignShipperCommand, OrderDetailDto>,
    IRequestHandler<ConfirmPickupCommand, OrderDetailDto>,
    IRequestHandler<ConfirmDeliveredCommand, OrderDetailDto>,
    IRequestHandler<FailDeliveryCommand, OrderDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IShippingCalculator _shipping;
    private readonly IDateTimeProvider _clock;

    public OrderActionHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IShippingCalculator shipping,
        IDateTimeProvider clock)
    {
        _context = context;
        _currentUser = currentUser;
        _shipping = shipping;
        _clock = clock;
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
            ChangeStatus(order, OrderStatus.AwaitingPickup, request.Note ?? "Đơn hàng đã được xác nhận.");
            return Task.CompletedTask;
        }, ct);

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
        }, ct);

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
        }, ct);

    public Task<OrderDetailDto> Handle(MarkPreparedCommand request, CancellationToken ct)
        => Transition(request.OrderId, order =>
        {
            EnsureSellerOrAdmin(order);
            if (order.Status != OrderStatus.AwaitingPickup)
            {
                throw new ConflictException("Order is not awaiting pickup.");
            }

            order.PreparedAt = _clock.UtcNow;
            if (order.Shipment is not null)
            {
                order.Shipment.SellerHandedOverAt = _clock.UtcNow;
                order.Shipment.PreparedByUserId = _currentUser.UserId;
            }

            AppendHistory(order, order.Status, order.Status, "Người bán đã chuẩn bị hàng.");
            return Task.CompletedTask;
        }, ct);

    public Task<OrderDetailDto> Handle(ClaimOrderCommand request, CancellationToken ct)
        => Transition(request.OrderId, async order =>
        {
            EnsureShipperOrAdmin();
            if (order.Status != OrderStatus.AwaitingPickup)
            {
                throw new ConflictException("Chỉ nhận được đơn đang ở trạng thái Chờ lấy hàng.");
            }

            if (order.PreparedAt is null)
            {
                throw new ConflictException(
                    "Người bán chưa bấm 'Đã chuẩn bị hàng'. Hãy chuẩn bị hàng trước khi nhận đơn giao.");
            }

            if (order.ShipperId is not null && order.ShipperId != _currentUser.UserId && !IsAdmin())
            {
                throw new ConflictException("Đơn đã được gán cho shipper khác.");
            }

            // Shipper tự nhận; Admin có thể tự nhận để xử lý (hoặc dùng assign-shipper).
            var shipperId = _currentUser.UserId!.Value;

            order.ShipperId = shipperId;
            if (order.Shipment is not null)
            {
                order.Shipment.ShipperId = shipperId;
                order.Shipment.CarrierName ??= IsAdmin() ? "PassDo Admin" : null;
            }

            AppendHistory(order, order.Status, order.Status, "Shipper đã nhận phân công đơn hàng.");
            await Task.CompletedTask;
        }, ct);

    public Task<OrderDetailDto> Handle(AssignShipperCommand request, CancellationToken ct)
        => Transition(request.OrderId, async order =>
        {
            if (!IsAdmin() && order.SellerId != _currentUser.UserId)
            {
                throw new ForbiddenException("Only seller or admin can assign shipper.");
            }

            if (order.Status != OrderStatus.AwaitingPickup)
            {
                throw new ConflictException("Shipper can only be assigned for awaiting-pickup orders.");
            }

            var shipper = await _context.Users.FirstOrDefaultAsync(x => x.Id == request.ShipperId && x.Role == UserRole.Shipper, ct)
                ?? throw new NotFoundException("Shipper", request.ShipperId);

            order.ShipperId = shipper.Id;
            if (order.Shipment is not null)
            {
                order.Shipment.ShipperId = shipper.Id;
                order.Shipment.CarrierName = shipper.FullName;
            }

            AppendHistory(order, order.Status, order.Status, $"Đã gán shipper {shipper.FullName}.");
        }, ct);

    public Task<OrderDetailDto> Handle(ConfirmPickupCommand request, CancellationToken ct)
        => Transition(request.OrderId, order =>
        {
            EnsureAssignedShipperOrAdmin(order);
            if (order.Status != OrderStatus.AwaitingPickup)
            {
                throw new ConflictException("Order is not awaiting pickup.");
            }

            if (order.ShipperId is null)
            {
                order.ShipperId = _currentUser.UserId;
            }

            var now = _clock.UtcNow;
            var eta = _shipping.CalculateEta(order.DeliverySpeed, now);
            order.PickedUpAt = now;
            order.EstimatedDeliveryFrom = eta.From;
            order.EstimatedDeliveryTo = eta.To;

            if (order.Shipment is not null)
            {
                order.Shipment.ShipperId = order.ShipperId;
                order.Shipment.ShipperReceivedAt = now;
                order.Shipment.EstimatedDeliveryFrom = eta.From;
                order.Shipment.EstimatedDeliveryTo = eta.To;
                order.Shipment.PickedUpConfirmedByUserId = _currentUser.UserId;
                if (!string.IsNullOrWhiteSpace(request.TrackingCode))
                {
                    order.Shipment.TrackingCode = request.TrackingCode.Trim();
                }
            }

            ChangeStatus(order, OrderStatus.Shipping, "Shipper đã nhận hàng từ người bán.");
            return Task.CompletedTask;
        }, ct);

    public Task<OrderDetailDto> Handle(ConfirmDeliveredCommand request, CancellationToken ct)
        => Transition(request.OrderId, async order =>
        {
            var isBuyer = order.BuyerId == _currentUser.UserId;
            if (!isBuyer)
            {
                EnsureAssignedShipperOrAdmin(order);
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
        }, ct);

    public Task<OrderDetailDto> Handle(FailDeliveryCommand request, CancellationToken ct)
        => Transition(request.OrderId, order =>
        {
            EnsureAssignedShipperOrAdmin(order);
            if (order.Status != OrderStatus.Shipping)
            {
                throw new ConflictException("Only shipping orders can fail delivery.");
            }

            order.CancellationReason = request.Reason;
            ChangeStatus(order, OrderStatus.DeliveryFailed, request.Reason);
            return Task.CompletedTask;
        }, ct);

    private async Task<OrderDetailDto> Transition(Guid orderId, Func<Order, Task> action, CancellationToken ct)
    {
        if (_currentUser.UserId is null)
        {
            throw new UnauthorizedException();
        }

        // Avoid Include of required principals (Buyer/Seller/Product via Items):
        // EF treats unloaded required navigations as severed and issues DELETE/UPDATE
        // that hit 0 rows under soft-delete filters → DbUpdateConcurrencyException.
        var order = await _context.Orders
            .Include(x => x.Payment)
            .Include(x => x.Shipment)
            .FirstOrDefaultAsync(x => x.Id == orderId, ct)
            ?? throw new NotFoundException("Order", orderId);

        EnsureShipmentStub(order);
        EnsurePaymentStub(order);

        await action(order);
        await _context.SaveChangesAsync(ct);

        var loaded = await _context.Orders
            .AsNoTracking()
            .Include(x => x.Items)
            .Include(x => x.Payment)
            .Include(x => x.Shipment)
            .Include(x => x.StatusHistories).ThenInclude(x => x.ChangedByUser)
            .Include(x => x.Buyer)
            .Include(x => x.Seller)
            .Include(x => x.Shipper)
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
            CarrierName = "PassDo Shipper",
            EstimatedDeliveryFrom = order.EstimatedDeliveryFrom,
            EstimatedDeliveryTo = order.EstimatedDeliveryTo
        };

        // Must Add explicitly: BaseEntity pre-assigns Id, so graph attach via navigation
        // would be treated as Modified → UPDATE 0 rows.
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
    private bool IsShipper() => string.Equals(_currentUser.Role, Roles.Shipper, StringComparison.OrdinalIgnoreCase);

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

    private void EnsureShipperOrAdmin()
    {
        if (!IsShipper() && !IsAdmin())
        {
            throw new ForbiddenException("Only shipper or admin can perform this action.");
        }
    }

    private void EnsureAssignedShipperOrAdmin(Order order)
    {
        if (IsAdmin())
        {
            return;
        }

        if (!IsShipper() || order.ShipperId != _currentUser.UserId)
        {
            if (IsShipper() && order.ShipperId is null)
            {
                order.ShipperId = _currentUser.UserId;
                if (order.Shipment is not null)
                {
                    order.Shipment.ShipperId = _currentUser.UserId;
                }

                return;
            }

            throw new ForbiddenException("Only the assigned shipper or admin can perform this action.");
        }
    }

    private bool IsParticipant(Order order)
        => order.BuyerId == _currentUser.UserId
           || order.SellerId == _currentUser.UserId
           || order.ShipperId == _currentUser.UserId
           || IsAdmin();
}
