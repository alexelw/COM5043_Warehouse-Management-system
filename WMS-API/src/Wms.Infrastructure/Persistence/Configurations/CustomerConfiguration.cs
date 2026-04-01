using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entities;

namespace Wms.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
  public void Configure(EntityTypeBuilder<Customer> builder)
  {
    builder.ToTable("customers");

    builder.HasKey(customer => customer.CustomerId);

    builder.Property(customer => customer.CustomerId)
        .HasColumnName("customer_id");

    builder.Property(customer => customer.Name)
        .HasColumnName("name")
        .HasMaxLength(256)
        .IsRequired();

    builder.OwnsOne(
        customer => customer.Contact,
        contact =>
        {
          // EF uses this optional-dependent marker to distinguish
          // between an absent contact and a contact whose values are null.
          contact.Property<bool>("contact_marker")
                  .HasColumnName("contact_marker");

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

    builder.Navigation(customer => customer.Contact)
        .IsRequired(false);
  }
}
