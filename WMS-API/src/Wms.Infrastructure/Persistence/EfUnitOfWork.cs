using Wms.Application.Abstractions;

namespace Wms.Infrastructure.Persistence;

public sealed class EfUnitOfWork : IUnitOfWork
{
  private readonly WmsDbContext _dbContext;

  public EfUnitOfWork(WmsDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public Task SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    return _dbContext.SaveChangesAsync(cancellationToken);
  }
}
