using System.Net.Mail;
using Wms.Domain.Exceptions;

namespace Wms.Domain.ValueObjects;

/// <summary>
/// Holds the contact details kept for a supplier or customer.
/// </summary>
public sealed record ContactDetails
{
  private ContactDetails()
  {
  }

  public ContactDetails(string? email, string? phone, string? address)
  {
    this.Email = Normalize(email);
    this.Phone = Normalize(phone);
    this.Address = Normalize(address);

    if (!this.HasAnyContactMethod)
    {
      throw new DomainRuleViolationException(
          "At least one contact method must be provided: email, phone, or address.");
    }

    if (this.Email is not null)
    {
      ValidateEmail(this.Email);
    }
  }

  public string? Email { get; init; }

  public string? Phone { get; init; }

  public string? Address { get; init; }

  public bool HasAnyContactMethod =>
      this.Email is not null ||
      this.Phone is not null ||
      this.Address is not null;

  public override string ToString()
  {
    return string.Join(
        ", ",
        new[] { this.Email, this.Phone, this.Address }.Where(static value => !string.IsNullOrWhiteSpace(value)));
  }

  private static string? Normalize(string? value)
  {
    return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
  }

  private static void ValidateEmail(string email)
  {
    try
    {
      _ = new MailAddress(email);
    }
    catch (FormatException ex)
    {
      throw new DomainRuleViolationException("Email address is not in a valid format.", ex);
    }
  }
}
