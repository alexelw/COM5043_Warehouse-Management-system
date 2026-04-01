using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entities;
using Wms.Domain.ValueObjects;

namespace Wms.Infrastructure.Persistence.Configurations;

public sealed class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
  public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
  {
    builder.ToTable("purchase_order_lines");

    builder.HasKey(line => line.PurchaseOrderLineId);

    builder.Property(line => line.PurchaseOrderLineId)
        .HasColumnName("purchase_order_line_id");

    builder.Property(line => line.PurchaseOrderId)
        .HasColumnName("purchase_order_id")
        .IsRequired();

    builder.Property(line => line.ProductId)
        .HasColumnName("product_id")
        .IsRequired();

    builder.HasOne<Product>()
        .WithMany()
        .HasForeignKey(line => line.ProductId)
        .OnDelete(DeleteBehavior.Restrict);

    builder.Property(line => line.QuantityOrdered)
        .HasColumnName("quantity_ordered")
        .IsRequired();

    builder.OwnsOne(
        line => line.UnitCostAtOrder,
        money =>
        {
          money.Property(value => value.Amount)
                  .HasColumnName("unit_cost_amount")
                  .HasPrecision(18, 2)
                  .IsRequired();

          money.Property(value => value.Currency)
                  .HasColumnName("unit_cost_currency")
                  .HasMaxLength(3)
                  .HasDefaultValue(Money.GbpCurrencyCode)
                  .IsRequired();
        });

    builder.Navigation(line => line.UnitCostAtOrder)
        .IsRequired();

    builder.HasIndex(line => line.ProductId);

    builder.Ignore(line => line.LineTotal);
  }
}
