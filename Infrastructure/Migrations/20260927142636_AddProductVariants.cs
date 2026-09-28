using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductVariant_ProductId",
                schema: "catalog",
                table: "ProductVariant");

            migrationBuilder.DropIndex(
                name: "IX_CartItem_CartId",
                schema: "dbo",
                table: "CartItem");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                schema: "catalog",
                table: "ProductVariant",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "catalog",
                table: "ProductVariant",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "Size",
                schema: "catalog",
                table: "ProductVariant",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VariantId",
                schema: "sales",
                table: "OrderItem",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantLabel",
                schema: "sales",
                table: "OrderItem",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ProductId",
                schema: "dbo",
                table: "CartItem",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "VariantId",
                schema: "dbo",
                table: "CartItem",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariant_ProductId_Size_Color",
                schema: "catalog",
                table: "ProductVariant",
                columns: new[] { "ProductId", "Size", "Color" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariant_SKU",
                schema: "catalog",
                table: "ProductVariant",
                column: "SKU",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_CartId_ProductId_VariantId",
                schema: "dbo",
                table: "CartItem",
                columns: new[] { "CartId", "ProductId", "VariantId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductVariant_ProductId_Size_Color",
                schema: "catalog",
                table: "ProductVariant");

            migrationBuilder.DropIndex(
                name: "IX_ProductVariant_SKU",
                schema: "catalog",
                table: "ProductVariant");

            migrationBuilder.DropIndex(
                name: "IX_CartItem_CartId_ProductId_VariantId",
                schema: "dbo",
                table: "CartItem");

            migrationBuilder.DropColumn(
                name: "Color",
                schema: "catalog",
                table: "ProductVariant");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "catalog",
                table: "ProductVariant");

            migrationBuilder.DropColumn(
                name: "Size",
                schema: "catalog",
                table: "ProductVariant");

            migrationBuilder.DropColumn(
                name: "VariantId",
                schema: "sales",
                table: "OrderItem");

            migrationBuilder.DropColumn(
                name: "VariantLabel",
                schema: "sales",
                table: "OrderItem");

            migrationBuilder.DropColumn(
                name: "VariantId",
                schema: "dbo",
                table: "CartItem");

            migrationBuilder.AlterColumn<string>(
                name: "ProductId",
                schema: "dbo",
                table: "CartItem",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariant_ProductId",
                schema: "catalog",
                table: "ProductVariant",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItem_CartId",
                schema: "dbo",
                table: "CartItem",
                column: "CartId");
        }
    }
}
