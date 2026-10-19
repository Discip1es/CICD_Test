using FluentAssertions;
using MyApp.Application.Services;
using MyApp.Domain.Entities;

namespace MyApp.Tests.Unit;

public class InMemoryCalculationHistoryStoreTests
{
    private static CalculationRecord MakeRecord(string expression = "1 + 1", decimal result = 2m) =>
        new(Guid.NewGuid(), expression, result, DateTimeOffset.UtcNow);

    [Fact]
    public async Task AddAsync_IncrementsCountAndKeepsOrder()
    {
        var store = new InMemoryCalculationHistoryStore();

        await store.AddAsync(MakeRecord("1 + 1", 2m));
        await store.AddAsync(MakeRecord("2 + 2", 4m));

        var all = await store.GetAllAsync();

        store.Count.Should().Be(2);
        all.Select(r => r.Expression).Should().Equal("1 + 1", "2 + 2");
    }

    [Fact]
    public async Task AddAsync_BeyondCapacity_EvictsOldestFifo()
    {
        var store = new InMemoryCalculationHistoryStore(capacity: 3);

        for (var i = 1; i <= 5; i++)
        {
            await store.AddAsync(MakeRecord($"{i}", i));
        }

        var all = await store.GetAllAsync();

        store.Count.Should().Be(3);
        all.Select(r => r.Expression).Should().Equal("3", "4", "5");
    }

    [Fact]
    public async Task ClearAsync_EmptiesStore()
    {
        var store = new InMemoryCalculationHistoryStore();
        await store.AddAsync(MakeRecord());

        await store.ClearAsync();

        store.Count.Should().Be(0);
        (await store.GetAllAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_ConcurrentWrites_AreThreadSafe()
    {
        var store = new InMemoryCalculationHistoryStore(capacity: 10_000);

        await Task.WhenAll(Enumerable.Range(0, 100)
            .Select(i => store.AddAsync(MakeRecord($"{i}", i))));

        store.Count.Should().Be(100);
    }

    [Fact]
    public void Constructor_NonPositiveCapacity_Throws()
    {
        var act = () => new InMemoryCalculationHistoryStore(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
