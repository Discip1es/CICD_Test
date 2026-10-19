using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MyApp.Api.Endpoints;
using MyApp.Domain.Parsing;

namespace MyApp.Tests.Integration;

public class CalculatorApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CalculatorApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("1 + 2", 3)]
    [InlineData("(2 + 3) * 4", 20)]
    [InlineData("2 ^ 8 % 7", 4)]
    public async Task Evaluate_ValidExpression_ReturnsResult(string expression, double expected)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/calculator/evaluate", new EvaluateRequest(expression));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<EvaluateResponse>();
        payload.Should().NotBeNull();
        ((double)payload!.Result).Should().BeApproximately(expected, 1e-9);
    }

    [Fact]
    public async Task Evaluate_DivisionByZero_ReturnsProblemDetails400()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/calculator/evaluate", new EvaluateRequest("1 / 0"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var problem = await response.Content.ReadFromJsonAsync<ProblemPayload>();
        problem!.Title.Should().Be("Division by zero");
    }

    [Fact]
    public async Task Evaluate_MalformedExpression_Returns400()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/calculator/evaluate", new EvaluateRequest("1 +"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task History_AccumulatesEvaluations_AndCanBeCleared()
    {
        // Isolated factory per test so the singleton store is fresh.
        using var client = _factory.WithWebHostBuilder(_ => { }).CreateClient();

        await client.PostAsJsonAsync("/api/calculator/evaluate", new EvaluateRequest("8 * 8"));

        var history = await client.GetFromJsonAsync<List<HistoryItem>>("/api/calculator/history");

        history.Should().NotBeNull();
        history!.Should().ContainSingle(h => h.Expression == "8 * 8");

        var clear = await client.DeleteAsync("/api/calculator/history");
        clear.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterClear = await client.GetFromJsonAsync<List<HistoryItem>>("/api/calculator/history");
        afterClear.Should().BeEmpty();
    }

    [Fact]
    public async Task Tokenize_ReturnsTokenStream()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/calculator/tokenize", new TokenizeRequest("1 + 2"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokens = await response.Content.ReadFromJsonAsync<List<TokenDto>>();
        tokens.Should().NotBeNull();
        tokens!.Select(t => t.Type).Should().Equal(
            nameof(TokenType.Number), nameof(TokenType.Plus), nameof(TokenType.Number));
    }

    // Local DTO mirroring ProblemDetails JSON shape.
    private sealed record ProblemPayload(string Title, string Detail, int Status);
}
