using FluentAssertions;
using MyApp.Domain.ValueObjects;
using MyApp.Domain.Exceptions;

namespace MyApp.Tests.Unit;

public class MoneyTests
{
    [Fact]
    public void Add_ReturnsSum()
    {
        var result = new Money(10.5m).Add(new Money(4.5m));

        result.Amount.Should().Be(15m);
    }

    [Fact]
    public void Subtract_ReturnsDifference()
    {
        new Money(10m).Subtract(new Money(3m)).Amount.Should().Be(7m);
    }

    [Fact]
    public void Multiply_AppliesFactor()
    {
        new Money(2m).Multiply(2.5m).Amount.Should().Be(5m);
    }

    [Fact]
    public void Divide_ByZero_ThrowsDomainDivisionByZero()
    {
        var act = () => new Money(1m).Divide(0m);

        act.Should().Throw<DivisionByZeroException>();
    }

    [Theory]
    [InlineData("123.45", true)]
    [InlineData("-7", true)]
    [InlineData("abc", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TryParse_HandlesInputs(string? input, bool expectedSuccess)
    {
        var success = Money.TryParse(input, out var money);

        success.Should().Be(expectedSuccess);

        if (expectedSuccess && input is not null)
        {
            money.ToString().Should().Be(decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture)
                .ToString("0.############################", System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    [Fact]
    public void Parse_InvalidInput_ThrowsFormat()
    {
        var act = () => Money.Parse("not-a-number");

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Equality_WorksByValue()
    {
        new Money(5m).Should().Be(new Money(5m));
        new Money(5m).Should().NotBe(new Money(6m));
        (new Money(5m) == new Money(5m)).Should().BeTrue();
        (new Money(5m) > new Money(4m)).Should().BeTrue();
    }

    [Fact]
    public void CompareTo_OrdersByAmount()
    {
        var list = new[] { new Money(3m), new Money(1m), new Money(2m) };

        list.OrderBy(m => m).Select(m => m.Amount).Should().Equal(1m, 2m, 3m);
    }
}
