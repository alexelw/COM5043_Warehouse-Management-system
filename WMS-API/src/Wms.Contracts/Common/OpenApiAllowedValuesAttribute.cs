namespace Wms.Contracts.Common;

[AttributeUsage(AttributeTargets.Property)]
public sealed class OpenApiAllowedValuesAttribute : Attribute
{
  public OpenApiAllowedValuesAttribute(params string[] values)
  {
    this.Values = values;
  }

  public IReadOnlyList<string> Values { get; }
}
