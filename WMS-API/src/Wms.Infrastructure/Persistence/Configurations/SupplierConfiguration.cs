using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entities;

namespace Wms.Infrastructure.Persistence.Configurations;

public sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
  public void Configure(EntityTypeBuilder<Supplier> builder)
  {
    builder.ToTable("suppliers");

    builder.HasKey(supplier => supplier.SupplierId);

    builder.Property(supplier => supplier.SupplierId)
        .HasColumnName("supplier_id");

    builder.Property(supplier => supplier.Name)
        .HasColumnName("name")
        .HasMaxLength(256)
        .IsRequired();

    builder.OwnsOne(
        supplier => supplier.Contact,
        contact =>
        {
          contact.Property(value => value.Email)
                  .HasColumnName("email")
                  .HasMaxLength(320);

          contact.Property(value => value.Phone)
                  .HasColumnName("phone")
                  .HasMaxLength(32);

          contact.Property(value => value.Address)
                  .HasColumnName("address")
                  .HasMaxLength(512);
        });

    builder.Navigation(supplier => supplier.Contact)
        .IsRequired();
  }
}
