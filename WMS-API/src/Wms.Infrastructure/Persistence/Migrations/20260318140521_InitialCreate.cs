using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wms.Infrastructure.Persistence.Migrations
{
  /// <inheritdoc />
  public partial class InitialCreate : Migration
  {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.AlterDatabase()
          .Annotation("MySql:CharSet", "utf8mb4");

      migrationBuilder.CreateTable(
          name: "customers",
          columns: table => new
          {
            customer_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                  .Annotation("MySql:CharSet", "utf8mb4"),
            email = table.Column<string>(type: "varchar(320)", maxLength: 320, nullable: true)
                  .Annotation("MySql:CharSet", "utf8mb4"),
            phone = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                  .Annotation("MySql:CharSet", "utf8mb4"),
            address = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                  .Annotation("MySql:CharSet", "utf8mb4")
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_customers", x => x.customer_id);
          })
          .Annotation("MySql:CharSet", "utf8mb4");

      migrationBuilder.CreateTable(
          name: "financial_transactions",
          columns: table => new
          {
            transaction_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            type = table.Column<int>(type: "int", nullable: false),
            amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
            currency = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false, defaultValue: "GBP")
                  .Annotation("MySql:CharSet", "utf8mb4"),
            status = table.Column<int>(type: "int", nullable: false),
            occurred_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            reference_type = table.Column<int>(type: "int", nullable: false),
            reference_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            reversal_of_transaction_id = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci")
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_financial_transactions", x => x.transaction_id);
            table.ForeignKey(
                      name: "FK_financial_transactions_financial_transactions_reversal_of_tr~",
                      column: x => x.reversal_of_transaction_id,
                      principalTable: "financial_transactions",
                      principalColumn: "transaction_id",
                      onDelete: ReferentialAction.Restrict);
          })
          .Annotation("MySql:CharSet", "utf8mb4");

      migrationBuilder.CreateTable(
          name: "report_exports",
          columns: table => new
          {
            report_export_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            report_type = table.Column<int>(type: "int", nullable: false),
            format = table.Column<int>(type: "int", nullable: false),
            generated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            file_path = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false)
                  .Annotation("MySql:CharSet", "utf8mb4"),
            range_from = table.Column<DateTime>(type: "datetime(6)", nullable: true),
            range_to = table.Column<DateTime>(type: "datetime(6)", nullable: true)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_report_exports", x => x.report_export_id);
          })
          .Annotation("MySql:CharSet", "utf8mb4");

      migrationBuilder.CreateTable(
          name: "suppliers",
          columns: table => new
          {
            supplier_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                  .Annotation("MySql:CharSet", "utf8mb4"),
            email = table.Column<string>(type: "varchar(320)", maxLength: 320, nullable: true)
                  .Annotation("MySql:CharSet", "utf8mb4"),
            phone = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                  .Annotation("MySql:CharSet", "utf8mb4"),
            address = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                  .Annotation("MySql:CharSet", "utf8mb4")
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_suppliers", x => x.supplier_id);
          })
          .Annotation("MySql:CharSet", "utf8mb4");

      migrationBuilder.CreateTable(
          name: "customer_orders",
          columns: table => new
          {
            customer_order_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            customer_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            status = table.Column<int>(type: "int", nullable: false),
            created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_customer_orders", x => x.customer_order_id);
            table.ForeignKey(
                      name: "FK_customer_orders_customers_customer_id",
                      column: x => x.customer_id,
                      principalTable: "customers",
                      principalColumn: "customer_id",
                      onDelete: ReferentialAction.Restrict);
          })
          .Annotation("MySql:CharSet", "utf8mb4");

      migrationBuilder.CreateTable(
          name: "products",
          columns: table => new
          {
            product_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            supplier_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            sku = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                  .Annotation("MySql:CharSet", "utf8mb4"),
            name = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                  .Annotation("MySql:CharSet", "utf8mb4"),
            reorder_threshold = table.Column<int>(type: "int", nullable: false),
            unit_cost_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
            unit_cost_currency = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false, defaultValue: "GBP")
                  .Annotation("MySql:CharSet", "utf8mb4"),
            quantity_on_hand = table.Column<int>(type: "int", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_products", x => x.product_id);
            table.ForeignKey(
                      name: "FK_products_suppliers_supplier_id",
                      column: x => x.supplier_id,
                      principalTable: "suppliers",
                      principalColumn: "supplier_id",
                      onDelete: ReferentialAction.Restrict);
          })
          .Annotation("MySql:CharSet", "utf8mb4");

      migrationBuilder.CreateTable(
          name: "purchase_orders",
          columns: table => new
          {
            purchase_order_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            supplier_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            status = table.Column<int>(type: "int", nullable: false),
            created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_purchase_orders", x => x.purchase_order_id);
            table.ForeignKey(
                      name: "FK_purchase_orders_suppliers_supplier_id",
                      column: x => x.supplier_id,
                      principalTable: "suppliers",
                      principalColumn: "supplier_id",
                      onDelete: ReferentialAction.Restrict);
          })
          .Annotation("MySql:CharSet", "utf8mb4");

      migrationBuilder.CreateTable(
          name: "customer_order_lines",
          columns: table => new
          {
            customer_order_line_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            customer_order_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            product_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            quantity = table.Column<int>(type: "int", nullable: false),
            unit_price_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
            unit_price_currency = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false, defaultValue: "GBP")
                  .Annotation("MySql:CharSet", "utf8mb4")
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_customer_order_lines", x => x.customer_order_line_id);
            table.ForeignKey(
                      name: "FK_customer_order_lines_customer_orders_customer_order_id",
                      column: x => x.customer_order_id,
                      principalTable: "customer_orders",
                      principalColumn: "customer_order_id",
                      onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                      name: "FK_customer_order_lines_products_product_id",
                      column: x => x.product_id,
                      principalTable: "products",
                      principalColumn: "product_id",
                      onDelete: ReferentialAction.Restrict);
          })
          .Annotation("MySql:CharSet", "utf8mb4");

      migrationBuilder.CreateTable(
          name: "stock_movements",
          columns: table => new
          {
            stock_movement_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            product_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            type = table.Column<int>(type: "int", nullable: false),
            quantity = table.Column<int>(type: "int", nullable: false),
            occurred_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
            reason = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true)
                  .Annotation("MySql:CharSet", "utf8mb4"),
            reference_type = table.Column<int>(type: "int", nullable: false),
            reference_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci")
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_stock_movements", x => x.stock_movement_id);
            table.ForeignKey(
                      name: "FK_stock_movements_products_product_id",
                      column: x => x.product_id,
                      principalTable: "products",
                      principalColumn: "product_id",
                      onDelete: ReferentialAction.Restrict);
          })
          .Annotation("MySql:CharSet", "utf8mb4");

      migrationBuilder.CreateTable(
          name: "goods_receipts",
          columns: table => new
          {
            goods_receipt_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            purchase_order_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            received_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_goods_receipts", x => x.goods_receipt_id);
            table.ForeignKey(
                      name: "FK_goods_receipts_purchase_orders_purchase_order_id",
                      column: x => x.purchase_order_id,
                      principalTable: "purchase_orders",
                      principalColumn: "purchase_order_id",
                      onDelete: ReferentialAction.Restrict);
          })
          .Annotation("MySql:CharSet", "utf8mb4");

      migrationBuilder.CreateTable(
          name: "purchase_order_lines",
          columns: table => new
          {
            purchase_order_line_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            purchase_order_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            product_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            quantity_ordered = table.Column<int>(type: "int", nullable: false),
            unit_cost_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
            unit_cost_currency = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false, defaultValue: "GBP")
                  .Annotation("MySql:CharSet", "utf8mb4")
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_purchase_order_lines", x => x.purchase_order_line_id);
            table.ForeignKey(
                      name: "FK_purchase_order_lines_products_product_id",
                      column: x => x.product_id,
                      principalTable: "products",
                      principalColumn: "product_id",
                      onDelete: ReferentialAction.Restrict);
            table.ForeignKey(
                      name: "FK_purchase_order_lines_purchase_orders_purchase_order_id",
                      column: x => x.purchase_order_id,
                      principalTable: "purchase_orders",
                      principalColumn: "purchase_order_id",
                      onDelete: ReferentialAction.Cascade);
          })
          .Annotation("MySql:CharSet", "utf8mb4");

      migrationBuilder.CreateTable(
          name: "goods_receipt_lines",
          columns: table => new
          {
            goods_receipt_line_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            goods_receipt_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            product_id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
            quantity_received = table.Column<int>(type: "int", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_goods_receipt_lines", x => x.goods_receipt_line_id);
            table.ForeignKey(
                      name: "FK_goods_receipt_lines_goods_receipts_goods_receipt_id",
                      column: x => x.goods_receipt_id,
                      principalTable: "goods_receipts",
                      principalColumn: "goods_receipt_id",
                      onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                      name: "FK_goods_receipt_lines_products_product_id",
                      column: x => x.product_id,
                      principalTable: "products",
                      principalColumn: "product_id",
                      onDelete: ReferentialAction.Restrict);
          })
          .Annotation("MySql:CharSet", "utf8mb4");

      migrationBuilder.CreateIndex(
          name: "IX_customer_order_lines_customer_order_id",
          table: "customer_order_lines",
          column: "customer_order_id");

      migrationBuilder.CreateIndex(
          name: "IX_customer_order_lines_product_id",
          table: "customer_order_lines",
          column: "product_id");

      migrationBuilder.CreateIndex(
          name: "IX_customer_orders_customer_id",
          table: "customer_orders",
          column: "customer_id");

      migrationBuilder.CreateIndex(
          name: "IX_financial_transactions_occurred_at",
          table: "financial_transactions",
          column: "occurred_at");

      migrationBuilder.CreateIndex(
          name: "IX_financial_transactions_reversal_of_transaction_id",
          table: "financial_transactions",
          column: "reversal_of_transaction_id");

      migrationBuilder.CreateIndex(
          name: "IX_goods_receipt_lines_goods_receipt_id",
          table: "goods_receipt_lines",
          column: "goods_receipt_id");

      migrationBuilder.CreateIndex(
          name: "IX_goods_receipt_lines_product_id",
          table: "goods_receipt_lines",
          column: "product_id");

      migrationBuilder.CreateIndex(
          name: "IX_goods_receipts_purchase_order_id",
          table: "goods_receipts",
          column: "purchase_order_id");

      migrationBuilder.CreateIndex(
          name: "IX_products_sku",
          table: "products",
          column: "sku",
          unique: true);

      migrationBuilder.CreateIndex(
          name: "IX_products_supplier_id",
          table: "products",
          column: "supplier_id");

      migrationBuilder.CreateIndex(
          name: "IX_purchase_order_lines_product_id",
          table: "purchase_order_lines",
          column: "product_id");

      migrationBuilder.CreateIndex(
          name: "IX_purchase_order_lines_purchase_order_id",
          table: "purchase_order_lines",
          column: "purchase_order_id");

      migrationBuilder.CreateIndex(
          name: "IX_purchase_orders_supplier_id",
          table: "purchase_orders",
          column: "supplier_id");

      migrationBuilder.CreateIndex(
          name: "IX_stock_movements_product_id_occurred_at",
          table: "stock_movements",
          columns: new[] { "product_id", "occurred_at" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "customer_order_lines");

      migrationBuilder.DropTable(
          name: "financial_transactions");

      migrationBuilder.DropTable(
          name: "goods_receipt_lines");

      migrationBuilder.DropTable(
          name: "purchase_order_lines");

      migrationBuilder.DropTable(
          name: "report_exports");

      migrationBuilder.DropTable(
          name: "stock_movements");

      migrationBuilder.DropTable(
          name: "customer_orders");

      migrationBuilder.DropTable(
          name: "goods_receipts");

      migrationBuilder.DropTable(
          name: "products");

      migrationBuilder.DropTable(
          name: "customers");

      migrationBuilder.DropTable(
          name: "purchase_orders");

      migrationBuilder.DropTable(
          name: "suppliers");
    }
  }
}
