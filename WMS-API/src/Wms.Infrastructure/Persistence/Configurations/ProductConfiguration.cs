using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entities;
using Wms.Domain.ValueObjects;

namespace Wms.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
  public void Configure(EntityTypeBuilder<Product> builder)
  {
    builder.ToTable("products");

    builder.HasKey(product => product.ProductId);

    builder.Property(product => product.ProductId)
        .HasColumnName("product_id");

    builder.Property(product => product.SupplierId)
        .HasColumnName("supplier_id")
        .IsRequired();

    builder.HasOne<Supplier>()
        .WithMany()
        .HasForeignKey(product => product.SupplierId)
        .OnDelete(DeleteBehavior.Restrict);

    builder.Property(product => product.Sku)
        .HasColumnName("sku")
        .HasMaxLength(64)
        .IsRequired();

    builder.HasIndex(product => product.Sku)
        .IsUnique();

    builder.Property(product => product.Name)
        .HasColumnName("name")
        .HasMaxLength(256)
        .IsRequired();

    builder.Property(product => product.ReorderLevel)
        .HasColumnName("reorder_threshold")
        .IsRequired();

    builder.Property(product => product.QuantityOnHand)
        .HasColumnName("quantity_on_hand")
        .IsRequired();

    builder.OwnsOne(
        product => product.UnitCost,
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

    builder.Navigation(product => product.UnitCost)
        .IsRequired();

    builder.Ignore(product => product.IsLowStock);
  }
}
