using FluentAssertions;
using MyApp.Domain.Exceptions;
using MyApp.Domain.Parsing;

namespace MyApp.Tests.Unit;

public class ExpressionTokenizerTests
{
    [Fact]
    public void Tokenize_SimpleSum_ProducesExpectedTokens()
    {
        var tokens = new ExpressionTokenizer("1 + 2").Tokenize();

        tokens.Select(t => t.Type).Should().Equal(
            TokenType.Number, TokenType.Plus, TokenType.Number, TokenType.End);

        tokens[0].Value.Should().Be(1m);
        tokens[2].Value.Should().Be(2m);
    }

    [Theory]
    [InlineData("+", TokenType.Plus)]
    [InlineData("-", TokenType.Minus)]
    [InlineData("*", TokenType.Multiply)]
    [InlineData("/", TokenType.Divide)]
    [InlineData("%", TokenType.Modulo)]
    [InlineData("^", TokenType.Power)]
    [InlineData("(", TokenType.LParen)]
    [InlineData(")", TokenType.RParen)]
    public void Tokenize_Operators_MappedCorrectly(string input, TokenType expected)
    {
        var tokens = new ExpressionTokenizer(input).Tokenize();

        tokens[0].Type.Should().Be(expected);
    }

    [Theory]
    [InlineData("3.14", 3.14)]
    [InlineData(".5", 0.5)]
    [InlineData("1e3", 1000)]
    [InlineData("1.5e-2", 0.015)]
    public void Tokenize_Numbers_ParsedWithInvariantCulture(string input, double expected)
    {
        var tokens = new ExpressionTokenizer(input).Tokenize();

        ((double)tokens[0].Value).Should().BeApproximately(expected, 1e-9);
    }

    [Fact]
    public void Tokenize_RecordsPositions()
    {
        var tokens = new ExpressionTokenizer("  7 + 8").Tokenize();

        tokens[0].Position.Should().Be(2);
        tokens[1].Position.Should().Be(4);
        tokens[2].Position.Should().Be(6);
    }

    [Fact]
    public void Tokenize_UnexpectedCharacter_ThrowsWithPosition()
    {
        var act = () => new ExpressionTokenizer("1 & 2").Tokenize();

        act.Should().Throw<InvalidExpressionException>()
            .WithMessage("*position 2*");
    }

    [Fact]
    public void Tokenize_EmptyString_ReturnsOnlyEnd()
    {
        var tokens = new ExpressionTokenizer("   ").Tokenize();

        tokens.Should().ContainSingle().Which.Type.Should().Be(TokenType.End);
    }
}
