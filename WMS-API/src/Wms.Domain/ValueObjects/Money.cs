using Wms.Domain.Exceptions;

namespace Wms.Domain.ValueObjects;

/// <summary>
/// Stores an immutable GBP amount.
/// </summary>
public sealed record Money
{
  public const string GbpCurrencyCode = "GBP";

  public static Money Zero { get; } = new(0m);

  private Money()
  {
    this.Amount = 0m;
    this.Currency = GbpCurrencyCode;
  }

  public Money(decimal amount)
      : this(amount, GbpCurrencyCode)
  {
  }

  public Money(decimal amount, string currency)
  {
    if (amount < 0m)
    {
      throw new DomainRuleViolationException("Money amount cannot be negative.");
    }

    var normalizedCurrency = NormalizeCurrency(currency);
    if (!string.Equals(normalizedCurrency, GbpCurrencyCode, StringComparison.Ordinal))
    {
      throw new DomainRuleViolationException("Currency must be GBP.");
    }

    this.Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    this.Currency = normalizedCurrency;
  }

  public decimal Amount { get; init; }

  public string Currency { get; init; } = GbpCurrencyCode;

  public bool IsZero => this.Amount == 0m;

  public static Money operator +(Money left, Money right) => left.Add(right);

  public static Money operator -(Money left, Money right) => left.Subtract(right);

  public static Money operator *(Money money, int multiplier) => money.Multiply(multiplier);

  public static Money operator *(int multiplier, Money money) => money.Multiply(multiplier);

  public Money Add(Money other)
  {
    EnsureSameCurrency(other);
    return new Money(this.Amount + other.Amount, this.Currency);
  }

  public Money Subtract(Money other)
  {
    EnsureSameCurrency(other);
    return new Money(this.Amount - other.Amount, this.Currency);
  }

  public Money Multiply(int multiplier)
  {
    if (multiplier < 0)
    {
      throw new DomainRuleViolationException("Money cannot be multiplied by a negative quantity.");
    }

    return new Money(this.Amount * multiplier, this.Currency);
  }

  public override string ToString() => $"{this.Currency} {this.Amount:0.00}";

  private static string NormalizeCurrency(string currency)
  {
    if (string.IsNullOrWhiteSpace(currency))
    {
      throw new DomainRuleViolationException("Currency is required.");
    }

    return currency.Trim().ToUpperInvariant();
  }

  private void EnsureSameCurrency(Money other)
  {
    ArgumentNullException.ThrowIfNull(other);

    if (!string.Equals(this.Currency, other.Currency, StringComparison.Ordinal))
    {
      throw new DomainRuleViolationException("Money operations require matching currencies.");
    }
  }
}
