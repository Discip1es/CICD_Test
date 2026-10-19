using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using MyApp.Application.Abstractions;
using MyApp.Application.Services;
using MyApp.Domain.Entities;
using MyApp.Domain.Exceptions;

namespace MyApp.Tests.Unit;

public class CalculatorServiceTests
{
    private readonly Mock<ICalculationHistoryStore> _storeMock = new();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
    private readonly CalculatorService _sut;

    public CalculatorServiceTests()
    {
        _sut = new CalculatorService(_storeMock.Object, _timeProvider);
    }

    [Fact]
    public async Task EvaluateAsync_ValidExpression_ReturnsResultAndRecordsHistory()
    {
        var result = await _sut.EvaluateAsync("2 + 3 * 4");

        result.Result.Should().Be(14m);
        result.Expression.Should().Be("2 + 3 * 4");

        _storeMock.Verify(s => s.AddAsync(
            It.Is<CalculationRecord>(r => r.Expression == "2 + 3 * 4" && r.Result == 14m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_RecordUsesTimeProvider()
    {
        await _sut.EvaluateAsync("1 + 1");

        _storeMock.Verify(s => s.AddAsync(
            It.Is<CalculationRecord>(r => r.CreatedAtUtc == _timeProvider.GetUtcNow()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateAsync_RecordToHistoryFalse_DoesNotPersist()
    {
        await _sut.EvaluateAsync("1 + 1", recordToHistory: false);

        _storeMock.Verify(s => s.AddAsync(It.IsAny<CalculationRecord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EvaluateAsync_EmptyExpression_ThrowsArgument(string expression)
    {
        var act = () => _sut.EvaluateAsync(expression);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EvaluateAsync_DivisionByZero_PropagatesDomainException()
    {
        var act = () => _sut.EvaluateAsync("1 / 0");

        await act.Should().ThrowAsync<DivisionByZeroException>();
    }

    [Fact]
    public async Task GetHistoryAsync_DelegatesToStore()
    {
        var expected = new List<CalculationRecord>
        {
            new(Guid.NewGuid(), "1 + 1", 2m, DateTimeOffset.UtcNow),
        };

        _storeMock
            .Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var history = await _sut.GetHistoryAsync();

        history.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task ClearHistoryAsync_DelegatesToStore()
    {
        await _sut.ClearHistoryAsync();

        _storeMock.Verify(s => s.ClearAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_NullStore_Throws()
    {
        var act = () => new CalculatorService(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
