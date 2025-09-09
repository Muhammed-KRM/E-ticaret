using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KeremProject1backend.Migrations.General
{
    /// <inheritdoc />
    public partial class AddGuestUserSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                schema: "General",
                table: "ShoppingCarts",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "General",
                table: "ShoppingCarts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "GuestEmail",
                schema: "General",
                table: "ShoppingCarts",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestId",
                schema: "General",
                table: "ShoppingCarts",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestName",
                schema: "General",
                table: "ShoppingCarts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GuestPhone",
                schema: "General",
                table: "ShoppingCarts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                schema: "General",
                table: "ShoppingCarts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                schema: "General",
                table: "Orders",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                schema: "General",
                table: "Orders",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerName",
                schema: "General",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhone",
                schema: "General",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_GuestId",
                schema: "General",
                table: "ShoppingCarts",
                column: "GuestId",
                unique: true,
                filter: "[GuestId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_UserId",
                schema: "General",
                table: "ShoppingCarts",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductId",
                schema: "General",
                table: "CartItems",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_Products_ProductId",
                schema: "General",
                table: "CartItems",
                column: "ProductId",
                principalSchema: "General",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_Products_ProductId",
                schema: "General",
                table: "CartItems");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_GuestId",
                schema: "General",
                table: "ShoppingCarts");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_UserId",
                schema: "General",
                table: "ShoppingCarts");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_ProductId",
                schema: "General",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "General",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "GuestEmail",
                schema: "General",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "GuestId",
                schema: "General",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "GuestName",
                schema: "General",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "GuestPhone",
                schema: "General",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "LastModified",
                schema: "General",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                schema: "General",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerName",
                schema: "General",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerPhone",
                schema: "General",
                table: "Orders");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                schema: "General",
                table: "ShoppingCarts",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                schema: "General",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
