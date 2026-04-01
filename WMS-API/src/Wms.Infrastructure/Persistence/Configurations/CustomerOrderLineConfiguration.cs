using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entities;
using Wms.Domain.ValueObjects;

namespace Wms.Infrastructure.Persistence.Configurations;

public sealed class CustomerOrderLineConfiguration : IEntityTypeConfiguration<CustomerOrderLine>
{
  public void Configure(EntityTypeBuilder<CustomerOrderLine> builder)
  {
    builder.ToTable("customer_order_lines");

    builder.HasKey(line => line.CustomerOrderLineId);

    builder.Property(line => line.CustomerOrderLineId)
        .HasColumnName("customer_order_line_id");

    builder.Property(line => line.CustomerOrderId)
        .HasColumnName("customer_order_id")
        .IsRequired();

    builder.Property(line => line.ProductId)
        .HasColumnName("product_id")
        .IsRequired();

    builder.HasOne<Product>()
        .WithMany()
        .HasForeignKey(line => line.ProductId)
        .OnDelete(DeleteBehavior.Restrict);

    builder.Property(line => line.Quantity)
        .HasColumnName("quantity")
        .IsRequired();

    builder.OwnsOne(
        line => line.UnitPriceAtSale,
        money =>
        {
          money.Property(value => value.Amount)
                  .HasColumnName("unit_price_amount")
                  .HasPrecision(18, 2)
                  .IsRequired();

          money.Property(value => value.Currency)
                  .HasColumnName("unit_price_currency")
                  .HasMaxLength(3)
                  .HasDefaultValue(Money.GbpCurrencyCode)
                  .IsRequired();
        });

    builder.Navigation(line => line.UnitPriceAtSale)
        .IsRequired();

    builder.HasIndex(line => line.ProductId);

    builder.Ignore(line => line.LineTotal);
  }
}
