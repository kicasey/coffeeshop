using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoffeeShopSimulation.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrinkOrders_AspNetUsers_LoyaltyUserId",
                table: "DrinkOrders");

            migrationBuilder.DropIndex(
                name: "IX_DrinkOrders_LoyaltyUserId",
                table: "DrinkOrders");

            migrationBuilder.DropColumn(
                name: "LoyaltyUserId",
                table: "DrinkOrders");

            migrationBuilder.CreateIndex(
                name: "IX_DrinkOrders_UserId",
                table: "DrinkOrders",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DrinkOrders_AspNetUsers_UserId",
                table: "DrinkOrders",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DrinkOrders_AspNetUsers_UserId",
                table: "DrinkOrders");

            migrationBuilder.DropIndex(
                name: "IX_DrinkOrders_UserId",
                table: "DrinkOrders");

            migrationBuilder.AddColumn<string>(
                name: "LoyaltyUserId",
                table: "DrinkOrders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DrinkOrders_LoyaltyUserId",
                table: "DrinkOrders",
                column: "LoyaltyUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DrinkOrders_AspNetUsers_LoyaltyUserId",
                table: "DrinkOrders",
                column: "LoyaltyUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
