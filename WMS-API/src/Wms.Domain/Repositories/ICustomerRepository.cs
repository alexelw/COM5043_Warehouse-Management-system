using Wms.Domain.Entities;

namespace Wms.Domain.Repositories;

/// <summary>
/// Handles storage and lookup for customers.
/// </summary>
public interface ICustomerRepository
{
  Task<Customer?> GetByIdAsync(Guid customerId, CancellationToken cancellationToken = default);

  Task<IReadOnlyList<Customer>> ListAsync(string? searchTerm = null, CancellationToken cancellationToken = default);

  Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

  Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
}
