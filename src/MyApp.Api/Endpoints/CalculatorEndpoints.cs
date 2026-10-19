using MyApp.Domain.Parsing;

namespace MyApp.Api.Endpoints;

/// <summary>
/// Minimal-API endpoints for the calculator engine.
/// </summary>
public static class CalculatorEndpoints
{
    public static IEndpointRouteBuilder MapCalculatorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/calculator").WithTags("Calculator");

        group.MapPost("/evaluate", EvaluateAsync).WithName("EvaluateExpression");
        group.MapGet("/history", GetHistoryAsync).WithName("GetHistory");
        group.MapDelete("/history", ClearHistoryAsync).WithName("ClearHistory");
        group.MapPost("/tokenize", Tokenize).WithName("TokenizeExpression");

        return app;
    }

    private static async Task<IResult> EvaluateAsync(
        EvaluateRequest request,
        Application.Services.CalculatorService service,
        CancellationToken cancellationToken)
    {
        var result = await service.EvaluateAsync(request.Expression, recordToHistory: true, cancellationToken);

        return Results.Ok(new EvaluateResponse(result.Expression, result.Result, result.Elapsed.TotalMilliseconds));
    }

    private static async Task<IResult> GetHistoryAsync(
        Application.Services.CalculatorService service,
        CancellationToken cancellationToken)
    {
        var history = await service.GetHistoryAsync(cancellationToken);

        return Results.Ok(history.Select(r => new HistoryItem(r.Id, r.Expression, r.Result, r.CreatedAtUtc)));
    }

    private static async Task<IResult> ClearHistoryAsync(
        Application.Services.CalculatorService service,
        CancellationToken cancellationToken)
    {
        await service.ClearHistoryAsync(cancellationToken);
        return Results.NoContent();
    }

    private static IResult Tokenize(TokenizeRequest request)
    {
        var tokens = new ExpressionTokenizer(request.Expression).Tokenize();

        return Results.Ok(tokens
            .Where(t => t.Type != TokenType.End)
            .Select(t => new TokenDto(t.Type.ToString(), t.Value, t.Position)));
    }
}

public sealed record EvaluateRequest(string Expression);

public sealed record EvaluateResponse(string Expression, decimal Result, double ElapsedMs);

public sealed record HistoryItem(Guid Id, string Expression, decimal Result, DateTimeOffset CreatedAtUtc);

public sealed record TokenizeRequest(string Expression);

public sealed record TokenDto(string Type, decimal Value, int Position);
