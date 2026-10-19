using Bogus;
using FluentAssertions;
using MyApp.Domain.Parsing;

namespace MyApp.Tests.Property;

/// <summary>
/// Property-based (fuzz) tests: generated valid expressions must always evaluate
/// to the same value as an independent reference implementation, and random
/// garbage input must never crash the parser with a non-domain exception.
/// </summary>
public class ExpressionPropertyTests
{
    private static void ApplyBinary(Stack<decimal> stack, Func<decimal, decimal, decimal> op)
    {
        var b = stack.Pop();
        var a = stack.Pop();
        stack.Push(op(a, b));
    }

    /// <summary>Independent recursive infix evaluator used as the oracle.</summary>
    private static decimal ReferenceEval(string expr)
    {
        var pos = 0;

        decimal ParseExpression()
        {
            var value = ParseTerm();

            while (true)
            {
                SkipWs();

                if (pos < expr.Length && expr[pos] == '+') { pos++; value += ParseTerm(); }
                else if (pos < expr.Length && expr[pos] == '-') { pos++; value -= ParseTerm(); }
                else return value;
            }
        }

        decimal ParseTerm()
        {
            var value = ParseAtom();

            while (true)
            {
                SkipWs();

                if (pos < expr.Length && expr[pos] == '*') { pos++; value *= ParseAtom(); }
                else return value;
            }
        }

        decimal ParseAtom()
        {
            SkipWs();

            if (pos < expr.Length && expr[pos] == '(')
            {
                pos++;
                var inner = ParseExpression();
                SkipWs();
                pos++; // consume ')'
                return inner;
            }

            var start = pos;

            while (pos < expr.Length && (char.IsDigit(expr[pos]) || expr[pos] == '.'))
            {
                pos++;
            }

            return decimal.Parse(expr.AsSpan(start, pos - start), System.Globalization.CultureInfo.InvariantCulture);
        }

        void SkipWs()
        {
            while (pos < expr.Length && char.IsWhiteSpace(expr[pos]))
            {
                pos++;
            }
        }

        return ParseExpression();
    }

    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(150)]
    public void GeneratedValidExpressions_EvaluateToReferenceValue(int seedShift)
    {
        var randomizer = new Randomizer(seedShift);
        const int iterations = 200;

        for (var i = 0; i < iterations; i++)
        {
            var leafCount = randomizer.Int(2, 5);

            var values = Enumerable.Range(0, leafCount)
                .Select(_ => randomizer.Number(1, 40).ToString(System.Globalization.CultureInfo.InvariantCulture))
                .ToList();

            var ops = Enumerable.Range(1, leafCount - 1)
                .Select(_ => new[] { "+", "-", "*" }[randomizer.Int(0, 2)])
                .ToList();

            // Random binary tree in infix notation: leaves and operators are
            // shuffled independently, so each run explores a different parse shape.
            var order = randomizer.Shuffle(values).ToList();
            var expr = $"({order[0]}";
            for (var j = 0; j < ops.Count; j++)
            {
                expr += $" {ops[j]} ({order[j + 1]}";
            }

            expr += new string(')', ops.Count + 1);

            var expected = ReferenceEval(expr);
            var actual = ExpressionParser.Evaluate(expr);

            actual.Should().Be(expected, because: $"expression '{expr}' (iteration {i}, seed {seedShift})");
        }
    }

    [Theory]
    [InlineData(300)]
    [InlineData(600)]
    public void RandomGarbageInput_ThrowsOnlyDomainExceptions(int seed)
    {
        var faker = new Faker { Random = new Randomizer(seed) };

        for (var i = 0; i < 200; i++)
        {
            var garbage = string.Concat(faker.Lorem.Sentence(), " ", faker.Random.Words());

            var act = () => ExpressionParser.Evaluate(garbage);

            act.Should().Throw<Exception>(because: $"garbage input '{garbage}' must be rejected")
                .Where(ex => ex is MyApp.Domain.Exceptions.DomainException
                             || ex is FormatException
                             || ex is ArgumentException
                             || ex is InvalidOperationException);
        }
    }
}
