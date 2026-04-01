using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entities;

namespace Wms.Infrastructure.Persistence.Configurations;

public sealed class GoodsReceiptLineConfiguration : IEntityTypeConfiguration<GoodsReceiptLine>
{
  public void Configure(EntityTypeBuilder<GoodsReceiptLine> builder)
  {
    builder.ToTable("goods_receipt_lines");

    builder.HasKey(line => line.GoodsReceiptLineId);

    builder.Property(line => line.GoodsReceiptLineId)
        .HasColumnName("goods_receipt_line_id");

    builder.Property(line => line.GoodsReceiptId)
        .HasColumnName("goods_receipt_id")
        .IsRequired();

    builder.Property(line => line.ProductId)
        .HasColumnName("product_id")
        .IsRequired();

    builder.HasOne<Product>()
        .WithMany()
        .HasForeignKey(line => line.ProductId)
        .OnDelete(DeleteBehavior.Restrict);

    builder.Property(line => line.QuantityReceived)
        .HasColumnName("quantity_received")
        .IsRequired();
  }
}
