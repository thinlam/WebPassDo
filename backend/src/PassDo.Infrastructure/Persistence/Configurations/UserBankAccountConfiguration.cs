using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PassDo.Domain.Entities;

namespace PassDo.Infrastructure.Persistence.Configurations;

public class UserBankAccountConfiguration : IEntityTypeConfiguration<UserBankAccount>
{
    public void Configure(EntityTypeBuilder<UserBankAccount> builder)
    {
        builder.ToTable("UserBankAccounts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BankName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.AccountNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.AccountHolderName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Branch).HasMaxLength(200);

        builder.HasOne(x => x.User)
            .WithMany(x => x.BankAccounts)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.UserId);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
