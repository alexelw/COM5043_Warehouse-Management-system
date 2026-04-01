using Wms.Domain.Exceptions;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Entities;

public class Supplier
{
  private Supplier()
  {
  }

  public Supplier(string name, ContactDetails contact)
  {
    this.SupplierId = Guid.NewGuid();
    this.UpdateDetails(name, contact);
  }

  public Guid SupplierId { get; private set; }

  public string Name { get; private set; } = string.Empty;

  public ContactDetails Contact { get; private set; } = null!;

  public void UpdateDetails(string name, ContactDetails contact)
  {
    ArgumentNullException.ThrowIfNull(contact);

    this.Name = NormalizeRequired(name, "Supplier name is required.");
    this.Contact = contact;
  }

  private static string NormalizeRequired(string value, string message)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      throw new DomainRuleViolationException(message);
    }

    return value.Trim();
  }
}
