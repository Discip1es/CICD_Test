using System.Globalization;
using MyApp.Domain.Exceptions;

namespace MyApp.Domain.Parsing;

public enum TokenType
{
    Number,
    Plus,
    Minus,
    Multiply,
    Divide,
    Modulo,
    Power,
    LParen,
    RParen,
    End,
}

public readonly record struct Token(TokenType Type, decimal Value, int Position);

/// <summary>
/// Converts an arithmetic expression into a stream of tokens.
/// Supports numbers (integer/decimal/exponent), + - * / % ^ and parentheses.
/// </summary>
public sealed class ExpressionTokenizer
{
    private readonly string _input;
    private int _index;

    public ExpressionTokenizer(string input)
    {
        _input = input ?? throw new InvalidExpressionException("Expression is null.");
    }

    public IReadOnlyList<Token> Tokenize()
    {
        var tokens = new List<Token>();

        while (true)
        {
            SkipWhitespace();

            if (_index >= _input.Length)
            {
                tokens.Add(new Token(TokenType.End, 0m, _index));
                return tokens;
            }

            var start = _index;
            var c = _input[_index];

            switch (c)
            {
                case '+':
                    tokens.Add(new Token(TokenType.Plus, 0m, start));
                    _index++;
                    break;
                case '-':
                    tokens.Add(new Token(TokenType.Minus, 0m, start));
                    _index++;
                    break;
                case '*':
                    tokens.Add(new Token(TokenType.Multiply, 0m, start));
                    _index++;
                    break;
                case '/':
                    tokens.Add(new Token(TokenType.Divide, 0m, start));
                    _index++;
                    break;
                case '%':
                    tokens.Add(new Token(TokenType.Modulo, 0m, start));
                    _index++;
                    break;
                case '^':
                    tokens.Add(new Token(TokenType.Power, 0m, start));
                    _index++;
                    break;
                case '(':
                    tokens.Add(new Token(TokenType.LParen, 0m, start));
                    _index++;
                    break;
                case ')':
                    tokens.Add(new Token(TokenType.RParen, 0m, start));
                    _index++;
                    break;
                default:
                    if (char.IsDigit(c) || c == '.')
                    {
                        tokens.Add(ReadNumber(start));
                    }
                    else
                    {
                        throw new InvalidExpressionException(
                            $"Unexpected character '{c}'.", start);
                    }

                    break;
            }
        }
    }

    private Token ReadNumber(int start)
    {
        while (_index < _input.Length && char.IsDigit(_input[_index]))
        {
            _index++;
        }

        if (_index < _input.Length && _input[_index] == '.')
        {
            _index++;
            while (_index < _input.Length && char.IsDigit(_input[_index]))
            {
                _index++;
            }
        }

        // Scientific notation: 1.5e-3
        if (_index < _input.Length && (_input[_index] == 'e' || _input[_index] == 'E'))
        {
            var save = _index;
            _index++;
            if (_index < _input.Length && (_input[_index] == '+' || _input[_index] == '-'))
            {
                _index++;
            }

            if (_index < _input.Length && char.IsDigit(_input[_index]))
            {
                while (_index < _input.Length && char.IsDigit(_input[_index]))
                {
                    _index++;
                }
            }
            else
            {
                _index = save;
            }
        }

        var text = _input.Substring(start, _index - start);

        if (!decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            throw new InvalidExpressionException($"'{text}' is not a valid number.", start);
        }

        return new Token(TokenType.Number, value, start);
    }

    private void SkipWhitespace()
    {
        while (_index < _input.Length && char.IsWhiteSpace(_input[_index]))
        {
            _index++;
        }
    }
}
