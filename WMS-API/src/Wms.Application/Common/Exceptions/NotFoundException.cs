namespace Wms.Application.Common.Exceptions;

public class NotFoundException : Exception
{
  public NotFoundException(string resourceName, Guid resourceId)
      : base($"{resourceName} '{resourceId}' was not found.")
  {
    this.ResourceName = resourceName;
    this.ResourceId = resourceId;
  }

  public string ResourceName { get; }

  public Guid ResourceId { get; }
}
