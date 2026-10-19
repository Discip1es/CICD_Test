using System.Globalization;
using MyApp.Domain.Exceptions;

namespace MyApp.Domain.Parsing;

/// <summary>
/// Recursive-descent parser + evaluator for arithmetic expressions.
///
/// Grammar:
///   expression := term (('+' | '-') term)*
///   term       := factorWithPower (('*' | '/' | '%') factorWithPower)*
///   factorWithPower := atom ('^' factorWithPower)?   // right-associative power
///   atom       := ['+' | '-'] (number | '(' expression ')')
///
/// Overflow in any operation raises <see cref="OverflowException"/>.
/// </summary>
public sealed class ExpressionParser
{
    private const int MaxPowerExponent = 1000;

    private readonly IReadOnlyList<Token> _tokens;
    private int _position;

    public ExpressionParser(IReadOnlyList<Token> tokens)
    {
        _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
    }

    public static decimal Evaluate(string expression)
    {
        var tokens = new ExpressionTokenizer(expression).Tokenize();
        return new ExpressionParser(tokens).ParseAndExpectEnd();
    }

    private Token Current => _tokens[_position];

    private Token Advance() => _tokens[_position++];

    public decimal ParseAndExpectEnd()
    {
        var result = ParseExpression();

        if (Current.Type != TokenType.End)
        {
            throw new InvalidExpressionException(
                $"Unexpected token '{Describe(Current)}'.", Current.Position);
        }

        return result;
    }

    private decimal ParseExpression()
    {
        var result = ParseTerm();

        while (Current.Type is TokenType.Plus or TokenType.Minus)
        {
            var op = Advance();
            var rhs = ParseTerm();
            result = op.Type == TokenType.Plus ? CheckedAdd(result, rhs) : CheckedSubtract(result, rhs);
        }

        return result;
    }

    private decimal ParseTerm()
    {
        var result = ParseFactorWithPower();

        while (Current.Type is TokenType.Multiply or TokenType.Divide or TokenType.Modulo)
        {
            var op = Advance();
            var rhs = ParseFactorWithPower();

            result = op.Type switch
            {
                TokenType.Multiply => CheckedMultiply(result, rhs),
                TokenType.Divide => CheckedDivide(result, rhs),
                TokenType.Modulo => CheckedModulo(result, rhs),
                _ => throw new InvalidOperationException("Unhandled operator."),
            };
        }

        return result;
    }

    private decimal ParseFactorWithPower()
    {
        var baseValue = ParseAtom();

        // '^' is right-associative: 2^3^2 == 2^(3^2)
        if (Current.Type == TokenType.Power)
        {
            Advance();
            var exponent = ParseFactorWithPower();
            return CheckedPow(baseValue, exponent);
        }

        return baseValue;
    }

    private decimal ParseAtom()
    {
        var token = Current;

        switch (token.Type)
        {
            case TokenType.Number:
                Advance();
                return token.Value;

            case TokenType.Plus:
                Advance();
                return ParseAtom();

            case TokenType.Minus:
                Advance();
                return CheckedNegate(ParseAtom());

            case TokenType.LParen:
                Advance();
                var inner = ParseExpression();
                if (Current.Type != TokenType.RParen)
                {
                    throw new InvalidExpressionException(
                        "Unbalanced parenthesis: expected ')'.", Current.Position);
                }

                Advance();
                return inner;

            default:
                throw new InvalidExpressionException(
                    $"Expected a number or '(', but found '{Describe(token)}'.", token.Position);
        }
    }

    private static string Describe(Token token) => token.Type switch
    {
        TokenType.End => "end of expression",
        TokenType.Number => token.Value.ToString(CultureInfo.InvariantCulture),
        TokenType.Plus => "+",
        TokenType.Minus => "-",
        TokenType.Multiply => "*",
        TokenType.Divide => "/",
        TokenType.Modulo => "%",
        TokenType.Power => "^",
        TokenType.LParen => "(",
        TokenType.RParen => ")",
        _ => token.Type.ToString(),
    };

    private static decimal CheckedAdd(decimal a, decimal b)
    {
        try
        {
            return checked(a + b);
        }
        catch (System.OverflowException)
        {
            throw new Exceptions.OverflowException("addition");
        }
    }

    private static decimal CheckedSubtract(decimal a, decimal b)
    {
        try
        {
            return checked(a - b);
        }
        catch (System.OverflowException)
        {
            throw new Exceptions.OverflowException("subtraction");
        }
    }

    private static decimal CheckedMultiply(decimal a, decimal b)
    {
        try
        {
            return checked(a * b);
        }
        catch (System.OverflowException)
        {
            throw new Exceptions.OverflowException("multiplication");
        }
    }

    private static decimal CheckedDivide(decimal a, decimal b)
    {
        if (b == 0m)
        {
            throw new DivisionByZeroException(a.ToString(CultureInfo.InvariantCulture), "0");
        }

        try
        {
            return checked(a / b);
        }
        catch (System.OverflowException)
        {
            throw new Exceptions.OverflowException("division");
        }
    }

    private static decimal CheckedModulo(decimal a, decimal b)
    {
        if (b == 0m)
        {
            throw new DivisionByZeroException(a.ToString(CultureInfo.InvariantCulture), "0");
        }

        return a % b;
    }

    private static decimal CheckedNegate(decimal a)
    {
        try
        {
            return checked(-a);
        }
        catch (System.OverflowException)
        {
            throw new Exceptions.OverflowException("negation");
        }
    }

    private static decimal CheckedPow(decimal baseValue, decimal exponent)
    {
        if (exponent != decimal.Truncate(exponent))
        {
            throw new InvalidExpressionException(
                $"Fractional exponents are not supported: {exponent.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (exponent > MaxPowerExponent || exponent < -MaxPowerExponent)
        {
            throw new Exceptions.OverflowException("exponentiation");
        }

        var asDouble = Math.Pow((double)baseValue, (double)exponent);

        if (double.IsNaN(asDouble) || double.IsInfinity(asDouble)
            || asDouble > (double)decimal.MaxValue || asDouble < (double)decimal.MinValue)
        {
            throw new Exceptions.OverflowException("exponentiation");
        }

        try
        {
            return checked((decimal)asDouble);
        }
        catch (System.OverflowException)
        {
            throw new Exceptions.OverflowException("exponentiation");
        }
    }
}
