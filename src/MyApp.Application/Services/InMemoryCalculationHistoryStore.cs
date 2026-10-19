using System.Collections.Concurrent;
using MyApp.Application.Abstractions;
using MyApp.Domain.Entities;

namespace MyApp.Application.Services;

/// <summary>
/// Thread-safe in-memory implementation of <see cref="ICalculationHistoryStore"/>
/// with a bounded capacity (oldest entries are evicted FIFO).
/// </summary>
public sealed class InMemoryCalculationHistoryStore : ICalculationHistoryStore
{
    private readonly ConcurrentQueue<CalculationRecord> _records = new();
    private readonly int _capacity;

    public InMemoryCalculationHistoryStore(int capacity = 1000)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _capacity = capacity;
    }

    public int Count => _records.Count;

    public Task AddAsync(CalculationRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        _records.Enqueue(record);

        while (_records.Count > _capacity && _records.TryDequeue(out _))
        {
            // Evict oldest to respect the bounded capacity.
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<CalculationRecord>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CalculationRecord>>(_records.ToArray());

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        while (_records.TryDequeue(out _))
        {
            // Drain the queue.
        }

        return Task.CompletedTask;
    }
}
