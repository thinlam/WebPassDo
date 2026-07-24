using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PassDo.Domain.Entities;

namespace PassDo.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderCode).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ProductTotal).HasPrecision(18, 2);
        builder.Property(x => x.ShippingFee).HasPrecision(18, 2);
        builder.Property(x => x.GrandTotal).HasPrecision(18, 2);
        builder.Property(x => x.Price).HasPrecision(18, 2);

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.DeliverySpeed).HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.Property(x => x.CancellationReason).HasMaxLength(1000);

        builder.Property(x => x.ShippingRecipientName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ShippingPhone).HasMaxLength(30).IsRequired();
        builder.Property(x => x.ShippingProvince).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ShippingDistrict).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ShippingWard).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ShippingStreetAddress).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ShippingAddressNote).HasMaxLength(500);

        builder.Property(x => x.PickupRecipientName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PickupPhone).HasMaxLength(30).IsRequired();
        builder.Property(x => x.PickupProvince).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PickupDistrict).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PickupWard).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PickupStreetAddress).HasMaxLength(500).IsRequired();

        builder.Property(x => x.BankNameSnapshot).HasMaxLength(150);
        builder.Property(x => x.BankAccountNumberSnapshot).HasMaxLength(50);
        builder.Property(x => x.BankAccountHolderSnapshot).HasMaxLength(200);
        builder.Property(x => x.BankBranchSnapshot).HasMaxLength(200);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Buyer)
            .WithMany(x => x.Purchases)
            .HasForeignKey(x => x.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Seller)
            .WithMany(x => x.Sales)
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Shipper)
            .WithMany(x => x.ShipmentsAssigned)
            .HasForeignKey(x => x.ShipperId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrderCode).IsUnique();
        builder.HasIndex(x => x.BuyerId);
        builder.HasIndex(x => x.SellerId);
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.ShipperId);
        builder.HasIndex(x => x.Status);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
