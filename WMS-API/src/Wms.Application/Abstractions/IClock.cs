namespace Wms.Application.Abstractions;

public interface IClock
{
  DateTime UtcNow { get; }
}
