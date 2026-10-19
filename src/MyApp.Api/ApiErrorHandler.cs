using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MyApp.Api;

/// <summary>
/// Maps domain/application exceptions to RFC 7807 Problem Details responses.
/// </summary>
public sealed class ApiErrorHandler : IExceptionHandler
{
    private readonly ILogger<ApiErrorHandler> _logger;

    public ApiErrorHandler(ILogger<ApiErrorHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            Domain.Exceptions.DivisionByZeroException => (StatusCodes.Status400BadRequest, "Division by zero"),
            Domain.Exceptions.OverflowException => (StatusCodes.Status400BadRequest, "Arithmetic overflow"),
            Domain.Exceptions.InvalidExpressionException => (StatusCodes.Status400BadRequest, "Invalid expression"),
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad request"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error"),
        };

        if (status >= 500)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning("Handled domain error on {Path}: {Message}", httpContext.Request.Path, exception.Message);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
        };

        problem.Type = "https://tools.ietf.org/html/rfc7807";
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = status;

        // Serialize to a buffer first: writing directly would leave the response
        // in a half-committed state if serialization fails. Then apply the
        // problem+json media type (WriteAsJsonAsync would override it).
        var json = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(problem, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        });

        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.Body.WriteAsync(json, cancellationToken);

        return true;
    }
}
