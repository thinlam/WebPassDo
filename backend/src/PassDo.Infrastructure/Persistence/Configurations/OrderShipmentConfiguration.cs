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

        builder.Property(x => x.DeliveryPersonName).HasMaxLength(200);
        builder.Property(x => x.DeliveryPersonPhone).HasMaxLength(30);
        builder.Property(x => x.DeliveryCompany).HasMaxLength(150);
        builder.Property(x => x.VehicleNumber).HasMaxLength(50);
        builder.Property(x => x.CarrierName).HasMaxLength(150);
        builder.Property(x => x.TrackingCode).HasMaxLength(100);
        builder.Property(x => x.DeliverySpeed).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.SenderCity).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ReceiverCity).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SenderDistrict).HasMaxLength(100);
        builder.Property(x => x.ReceiverDistrict).HasMaxLength(100);
        builder.Property(x => x.ShippingFee).HasPrecision(18, 2);
        builder.Property(x => x.DeliveryNote).HasMaxLength(1000);

        builder.HasOne(x => x.Order)
            .WithOne(x => x.Shipment)
            .HasForeignKey<OrderShipment>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");
        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Buyer)
            .WithMany()
            .HasForeignKey(x => x.BuyerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Seller)
            .WithMany()
            .HasForeignKey(x => x.SellerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ProductId, x.BuyerId, x.SellerId }).IsUnique();
        builder.HasIndex(x => x.BuyerId);
        builder.HasIndex(x => x.SellerId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Content).HasMaxLength(4000).IsRequired();

        builder.HasOne(x => x.Conversation)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Sender)
            .WithMany()
            .HasForeignKey(x => x.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
