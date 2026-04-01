namespace Wms.Api.Infrastructure;

internal sealed class RequestValidationException : Exception
{
  public RequestValidationException(
      IReadOnlyDictionary<string, IReadOnlyList<string>> errors,
      string message = "One or more validation errors occurred.")
      : base(message)
  {
    this.Errors = errors;
  }

  public IReadOnlyDictionary<string, IReadOnlyList<string>> Errors { get; }

  public static RequestValidationException ForSingleError(string field, string message)
  {
    return new RequestValidationException(new Dictionary<string, IReadOnlyList<string>>
    {
      [field] = new[] { message },
    });
  }
}
