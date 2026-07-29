using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PassDo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OneActiveOrderPerProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE Products SET Status = N'Active' WHERE Status = N'Available';

CREATE UNIQUE INDEX UX_Orders_OneActivePerProduct
ON Orders(ProductId)
WHERE IsDeleted = 0 AND [Status] IN (
    N'AwaitingPayment',
    N'PendingConfirmation',
    N'AwaitingPreparation',
    N'AwaitingHandover',
    N'Shipping'
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX UX_Orders_OneActivePerProduct ON Orders;");
        }
    }
}
