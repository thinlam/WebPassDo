using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PassDo.Domain.Entities;

namespace PassDo.Infrastructure.Persistence.Configurations;

public class UserAddressConfiguration : IEntityTypeConfiguration<UserAddress>
{
    public void Configure(EntityTypeBuilder<UserAddress> builder)
    {
        builder.ToTable("UserAddresses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RecipientName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Province).HasMaxLength(100).IsRequired();
        builder.Property(x => x.District).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Ward).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ProvinceCode).HasMaxLength(20);
        builder.Property(x => x.DistrictCode).HasMaxLength(20);
        builder.Property(x => x.WardCode).HasMaxLength(20);
        builder.Property(x => x.StreetAddress).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.AddressType).HasConversion<string>().HasMaxLength(50).IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.Addresses)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UserId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
