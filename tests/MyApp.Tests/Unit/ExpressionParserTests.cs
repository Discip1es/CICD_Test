using FluentAssertions;
using MyApp.Domain.Exceptions;
using MyApp.Domain.Parsing;

namespace MyApp.Tests.Unit;

public class ExpressionParserTests
{
    [Theory]
    [InlineData("1 + 2", 3)]
    [InlineData("10 - 4 - 3", 3)]                 // left-associative subtraction
    [InlineData("2 * 3 + 4", 10)]                 // precedence
    [InlineData("2 + 3 * 4", 14)]
    [InlineData("(2 + 3) * 4", 20)]
    [InlineData("((1 + 2) * (3 + 4))", 21)]
    [InlineData("7 % 3", 1)]
    [InlineData("10 / 4", 2.5)]
    [InlineData("-5 + 10", 5)]
    [InlineData("--5", 5)]
    [InlineData("+7", 7)]
    [InlineData("2 ^ 10", 1024)]
    [InlineData("2 ^ 3 ^ 2", 512)]                // right-associative power: 2^(3^2)
    [InlineData("1.5 * 2", 3)]
    [InlineData("1e3 + 1", 1001)]
    public void Evaluate_ValidExpressions_ReturnExpected(string expression, double expected)
    {
        var result = ExpressionParser.Evaluate(expression);

        ((double)result).Should().BeApproximately(expected, 1e-9);
    }

    [Theory]
    [InlineData("1 +")]
    [InlineData("+")]
    [InlineData("1 2")]
    [InlineData("(1 + 2")]
    [InlineData("1 + 2)")]
    [InlineData("()")]
    [InlineData("*3")]
    public void Evaluate_MalformedExpressions_ThrowInvalidExpression(string expression)
    {
        var act = () => ExpressionParser.Evaluate(expression);

        act.Should().Throw<InvalidExpressionException>();
    }

    [Fact]
    public void Evaluate_DivisionByZero_ThrowsDomainDivisionByZero()
    {
        var act = () => ExpressionParser.Evaluate("1 / 0");

        act.Should().Throw<DivisionByZeroException>();
    }

    [Fact]
    public void Evaluate_ModuloByZero_ThrowsDomainDivisionByZero()
    {
        var act = () => ExpressionParser.Evaluate("5 % 0");

        act.Should().Throw<DivisionByZeroException>();
    }

    [Fact]
    public void Evaluate_IntegerOverflow_ThrowsDomainOverflow()
    {
        var max = decimal.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var act = () => ExpressionParser.Evaluate($"{max} + 1");

        act.Should().Throw<MyApp.Domain.Exceptions.OverflowException>();
    }

    [Fact]
    public void Evaluate_PowerOverflow_ThrowsDomainOverflow()
    {
        var act = () => ExpressionParser.Evaluate("10 ^ 9999");

        act.Should().Throw<MyApp.Domain.Exceptions.OverflowException>();
    }

    [Fact]
    public void Evaluate_FractionalExponent_ThrowsInvalidExpression()
    {
        var act = () => ExpressionParser.Evaluate("4 ^ 0.5");

        act.Should().Throw<InvalidExpressionException>();
    }

    [Fact]
    public void Evaluate_NestedDeepParentheses_Works()
    {
        var expression = string.Concat(Enumerable.Repeat("(", 50)) + "1 + 1" + string.Concat(Enumerable.Repeat(")", 50));

        ExpressionParser.Evaluate(expression).Should().Be(2m);
    }
}
