using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PassDo.Domain.Entities;

namespace PassDo.Infrastructure.Persistence.Configurations;

public class OrderPaymentConfiguration : IEntityTypeConfiguration<OrderPayment>
{
    public void Configure(EntityTypeBuilder<OrderPayment> builder)
    {
        builder.ToTable("OrderPayments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Method).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 2);
        builder.Property(x => x.TransferContent).HasMaxLength(100);
        builder.Property(x => x.ProofImageUrl).HasMaxLength(500);
        builder.Property(x => x.Note).HasMaxLength(1000);

        builder.HasOne(x => x.Order)
            .WithOne(x => x.Payment)
            .HasForeignKey<OrderPayment>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ConfirmedByUser)
            .WithMany()
            .HasForeignKey(x => x.ConfirmedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
