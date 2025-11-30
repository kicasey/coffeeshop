using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoffeeShopSimulation.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountFieldsAndLastBirthdayDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "DrinkOrders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscountType",
                table: "DrinkOrders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastBirthdayDiscountUsed",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "DrinkOrders");

            migrationBuilder.DropColumn(
                name: "DiscountType",
                table: "DrinkOrders");

            migrationBuilder.DropColumn(
                name: "LastBirthdayDiscountUsed",
                table: "AspNetUsers");
        }
    }
}
