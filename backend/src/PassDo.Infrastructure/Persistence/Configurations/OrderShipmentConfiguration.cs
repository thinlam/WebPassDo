using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PassDo.Domain.Entities;

namespace PassDo.Infrastructure.Persistence.Configurations;

public class OrderShipmentConfiguration : IEntityTypeConfiguration<OrderShipment>
{
    public void Configure(EntityTypeBuilder<OrderShipment> builder)
    {
        builder.ToTable("OrderShipments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CarrierName).HasMaxLength(150);
        builder.Property(x => x.TrackingCode).HasMaxLength(100);
        builder.Property(x => x.DeliverySpeed).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.SenderCity).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ReceiverCity).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ShippingFee).HasPrecision(18, 2);
        builder.Property(x => x.DeliveryNote).HasMaxLength(1000);

        builder.HasOne(x => x.Order)
            .WithOne(x => x.Shipment)
            .HasForeignKey<OrderShipment>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Shipper)
            .WithMany()
            .HasForeignKey(x => x.ShipperId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasIndex(x => x.ShipperId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
