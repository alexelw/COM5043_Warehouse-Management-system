using Wms.Domain.Exceptions;
using Wms.Domain.ValueObjects;

namespace Wms.Domain.Entities;

public class Customer
{
  private Customer()
  {
  }

  public Customer(string name, ContactDetails? contact = null)
  {
    this.CustomerId = Guid.NewGuid();
    this.UpdateDetails(name, contact);
  }

  public Guid CustomerId { get; private set; }

  public string Name { get; private set; } = string.Empty;

  public ContactDetails? Contact { get; private set; }

  public void UpdateDetails(string name, ContactDetails? contact = null)
  {
    this.Name = NormalizeRequired(name, "Customer name is required.");
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
