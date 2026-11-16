using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace saifRestaurantApi.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPricingAndTiming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "OrderTickets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReadyAt",
                table: "OrderTickets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "OrderTickets",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "OrderDishes",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "OrderTickets");

            migrationBuilder.DropColumn(
                name: "ReadyAt",
                table: "OrderTickets");

            migrationBuilder.DropColumn(
                name: "Total",
                table: "OrderTickets");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "OrderDishes");
        }
    }
}
