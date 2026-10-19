using System.Globalization;

namespace MyApp.Domain.ValueObjects;

/// <summary>
/// Immutable value object, wrapping a <see cref="decimal"/> with explicit
/// parsing/formatting rules and arithmetic that reports domain errors.
/// </summary>
public readonly struct Money : IEquatable<Money>, IComparable<Money>
{
    public static readonly Money Zero = new(0m);

    public decimal Amount { get; }

    public Money(decimal amount)
    {
        Amount = amount;
    }

    public static Money Parse(string input) =>
        TryParse(input, out var money)
            ? money
            : throw new FormatException($"Cannot parse '{input}' as Money.");

    public static bool TryParse(string? input, out Money money)
    {
        money = Zero;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
        {
            money = new Money(value);
            return true;
        }

        return false;
    }

    public Money Add(Money other) => new(Amount + other.Amount);

    public Money Subtract(Money other) => new(Amount - other.Amount);

    public Money Multiply(decimal factor) => new(Amount * factor);

    public Money Divide(decimal divisor)
    {
        if (divisor == 0m)
        {
            throw new Exceptions.DivisionByZeroException(Amount.ToString(CultureInfo.InvariantCulture), "0");
        }

        return new Money(Amount / divisor);
    }

    public bool Equals(Money other) => Amount == other.Amount;

    public override bool Equals(object? obj) => obj is Money other && Equals(other);

    public override int GetHashCode() => Amount.GetHashCode();

    public int CompareTo(Money other) => Amount.CompareTo(other.Amount);

    public override string ToString() => Amount.ToString("0.############################", CultureInfo.InvariantCulture);

    public static bool operator ==(Money left, Money right) => left.Equals(right);

    public static bool operator !=(Money left, Money right) => !left.Equals(right);

    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;

    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
}
