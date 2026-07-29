using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PassDo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyShipperFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShipperId",
                table: "OrderShipments");

            migrationBuilder.DropColumn(
                name: "ShipperReceivedAt",
                table: "OrderShipments");

            migrationBuilder.DropColumn(
                name: "ShipperId",
                table: "Orders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ShipperId",
                table: "OrderShipments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShipperReceivedAt",
                table: "OrderShipments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ShipperId",
                table: "Orders",
                type: "uniqueidentifier",
                nullable: true);
        }
    }
}
