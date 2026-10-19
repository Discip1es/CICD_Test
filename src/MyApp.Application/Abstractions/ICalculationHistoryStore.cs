using MyApp.Domain.Entities;

namespace MyApp.Application.Abstractions;

/// <summary>
/// Storage-agnostic contract for persisting calculation history.
/// Implemented in-memory for tests/dev, could be backed by a DB later.
/// </summary>
public interface ICalculationHistoryStore
{
    Task<IReadOnlyList<CalculationRecord>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(CalculationRecord record, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);

    int Count { get; }
}
