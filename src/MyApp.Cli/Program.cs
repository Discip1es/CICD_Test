using System.Globalization;
using MyApp.Application.Services;
using MyApp.Domain.Exceptions;

namespace MyApp.Cli;

/// <summary>
/// Interactive REPL + one-shot evaluation for the calculator engine.
/// Exit codes: 0 = success, 1 = domain error, 2 = usage error.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var service = new CalculatorService(new InMemoryCalculationHistoryStore());

        if (args.Length > 0)
        {
            return await RunOneShotAsync(service, args);
        }

        return await RunReplAsync(service);
    }

    private static async Task<int> RunOneShotAsync(CalculatorService service, string[] args)
    {
        var expression = string.Join(' ', args);
        return await EvaluateAndPrintAsync(service, expression);
    }

    private static async Task<int> RunReplAsync(CalculatorService service)
    {
        Console.WriteLine("MyApp calculator REPL. Type an expression, or:");
        Console.WriteLine("  history   - show calculation history");
        Console.WriteLine("  clear     - clear history");
        Console.WriteLine("  exit      - quit");

        while (true)
        {
            Console.Write("> ");
            var line = Console.ReadLine();

            if (line is null)
            {
                return 0; // EOF
            }

            var input = line.Trim();

            switch (input.ToLowerInvariant())
            {
                case "":
                    continue;
                case "exit":
                case "quit":
                    return 0;
                case "history":
                    await PrintHistoryAsync(service);
                    continue;
                case "clear":
                    await service.ClearHistoryAsync();
                    Console.WriteLine("History cleared.");
                    continue;
            }

            var exitCode = await EvaluateAndPrintAsync(service, input);

            if (exitCode != 0 && !Environment.UserInteractive)
            {
                return exitCode;
            }
        }
    }

    private static async Task<int> EvaluateAndPrintAsync(CalculatorService service, string expression)
    {
        try
        {
            var result = await service.EvaluateAsync(expression);
            Console.WriteLine(
                $"{result.Expression} = {result.Result.ToString(CultureInfo.InvariantCulture)} " +
                $"({result.Elapsed.TotalMilliseconds:F3} ms)");
            return 0;
        }
        catch (DomainException ex)
        {
            Console.Error.WriteLine($"Domain error: {ex.Message}");
            return 1;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine($"Usage error: {ex.Message}");
            return 2;
        }
    }

    private static async Task PrintHistoryAsync(CalculatorService service)
    {
        var history = await service.GetHistoryAsync();

        if (history.Count == 0)
        {
            Console.WriteLine("(history is empty)");
            return;
        }

        foreach (var record in history)
        {
            Console.WriteLine(record);
        }
    }
}
