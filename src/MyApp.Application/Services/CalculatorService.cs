using System.Diagnostics;
using MyApp.Application.Abstractions;
using MyApp.Domain.Entities;
using MyApp.Domain.Parsing;

namespace MyApp.Application.Services;

public sealed record EvaluationResult(
    string Expression,
    decimal Result,
    TimeSpan Elapsed);

/// <summary>
/// Application service orchestrating expression evaluation and history recording.
/// All domain errors propagate as exceptions; the presentation layer maps them to
/// user-friendly responses (HTTP problem details / CLI messages).
/// </summary>
public sealed class CalculatorService
{
    private readonly ICalculationHistoryStore _history;
    private readonly TimeProvider _timeProvider;

    public CalculatorService(ICalculationHistoryStore history, TimeProvider? timeProvider = null)
    {
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<EvaluationResult> EvaluateAsync(
        string expression,
        bool recordToHistory = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new ArgumentException("Expression cannot be empty.", nameof(expression));
        }

        var stopwatch = Stopwatch.StartNew();
        var result = ExpressionParser.Evaluate(expression.Trim());
        stopwatch.Stop();

        if (recordToHistory)
        {
            var record = new CalculationRecord(
                Guid.NewGuid(),
                expression.Trim(),
                result,
                _timeProvider.GetUtcNow());

            await _history.AddAsync(record, cancellationToken);
        }

        return new EvaluationResult(expression.Trim(), result, stopwatch.Elapsed);
    }

    public Task<IReadOnlyList<CalculationRecord>> GetHistoryAsync(CancellationToken cancellationToken = default) =>
        _history.GetAllAsync(cancellationToken);

    public Task ClearHistoryAsync(CancellationToken cancellationToken = default) =>
        _history.ClearAsync(cancellationToken);
}
