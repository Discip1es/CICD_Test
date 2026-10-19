using MyApp.Domain.Exceptions;

namespace MyApp.Domain.Entities;

/// <summary>
/// A single line in a calculation history: the original expression and its result.
/// </summary>
public sealed class CalculationRecord
{
    public Guid Id { get; }

    public string Expression { get; }

    public decimal Result { get; }

    public DateTimeOffset CreatedAtUtc { get; }

    public CalculationRecord(Guid id, string expression, decimal result, DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            throw new InvalidExpressionException("Expression cannot be empty.");
        }

        Id = id;
        Expression = expression;
        Result = result;
        CreatedAtUtc = createdAtUtc;
    }

    public override string ToString() =>
        $"{CreatedAtUtc:O} | {Expression} = {Result}";
}
