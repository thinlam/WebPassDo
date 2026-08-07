using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PassDo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameOrderStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Orders_OneActivePerProduct'
      AND object_id = OBJECT_ID(N'[dbo].[Orders]')
)
BEGIN
    DROP INDEX [UX_Orders_OneActivePerProduct] ON [dbo].[Orders];
END
");

            // Rename status strings in Orders table (stored as nvarchar via HasConversion<string>())
            migrationBuilder.Sql(@"
    UPDATE Orders SET [Status] = N'PendingSellerConfirmation' WHERE [Status] = N'PendingConfirmation';
    UPDATE Orders SET [Status] = N'Preparing'                 WHERE [Status] = N'AwaitingPreparation';
    UPDATE Orders SET [Status] = N'ReadyForShipment'          WHERE [Status] = N'AwaitingHandover';
");

            // Rename status strings in history table
            migrationBuilder.Sql(@"
    UPDATE OrderStatusHistories SET [OldStatus] = N'PendingSellerConfirmation' WHERE [OldStatus] = N'PendingConfirmation';
    UPDATE OrderStatusHistories SET [OldStatus] = N'Preparing'                 WHERE [OldStatus] = N'AwaitingPreparation';
    UPDATE OrderStatusHistories SET [OldStatus] = N'ReadyForShipment'          WHERE [OldStatus] = N'AwaitingHandover';

    UPDATE OrderStatusHistories SET [NewStatus] = N'PendingSellerConfirmation' WHERE [NewStatus] = N'PendingConfirmation';
    UPDATE OrderStatusHistories SET [NewStatus] = N'Preparing'                 WHERE [NewStatus] = N'AwaitingPreparation';
    UPDATE OrderStatusHistories SET [NewStatus] = N'ReadyForShipment'          WHERE [NewStatus] = N'AwaitingHandover';
");

            migrationBuilder.Sql(@"
CREATE UNIQUE INDEX UX_Orders_OneActivePerProduct
ON Orders(ProductId)
WHERE IsDeleted = 0 AND [Status] IN (
    N'AwaitingPayment',
    N'PendingSellerConfirmation',
    N'Preparing',
    N'ReadyForShipment',
    N'Shipping'
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_Orders_OneActivePerProduct'
      AND object_id = OBJECT_ID(N'[dbo].[Orders]')
)
BEGIN
    DROP INDEX [UX_Orders_OneActivePerProduct] ON [dbo].[Orders];
END
");

            migrationBuilder.Sql(@"
    UPDATE Orders SET [Status] = N'PendingConfirmation'  WHERE [Status] = N'PendingSellerConfirmation';
    UPDATE Orders SET [Status] = N'AwaitingPreparation'  WHERE [Status] = N'Preparing';
    UPDATE Orders SET [Status] = N'AwaitingHandover'     WHERE [Status] = N'ReadyForShipment';
");

            migrationBuilder.Sql(@"
    UPDATE OrderStatusHistories SET [OldStatus] = N'PendingConfirmation' WHERE [OldStatus] = N'PendingSellerConfirmation';
    UPDATE OrderStatusHistories SET [OldStatus] = N'AwaitingPreparation' WHERE [OldStatus] = N'Preparing';
    UPDATE OrderStatusHistories SET [OldStatus] = N'AwaitingHandover'    WHERE [OldStatus] = N'ReadyForShipment';

    UPDATE OrderStatusHistories SET [NewStatus] = N'PendingConfirmation' WHERE [NewStatus] = N'PendingSellerConfirmation';
    UPDATE OrderStatusHistories SET [NewStatus] = N'AwaitingPreparation' WHERE [NewStatus] = N'Preparing';
    UPDATE OrderStatusHistories SET [NewStatus] = N'AwaitingHandover'    WHERE [NewStatus] = N'ReadyForShipment';
");

            migrationBuilder.Sql(@"
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

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "Orders");
        }
    }
}
