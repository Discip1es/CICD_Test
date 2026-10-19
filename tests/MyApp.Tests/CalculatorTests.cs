using MyApp;
using Xunit;

namespace MyApp.Tests;

public class CalculatorTests
{
    private readonly Calculator _sut = new();

    [Theory]
    [InlineData(1, 2, 3)]
    [InlineData(-1, 1, 0)]
    [InlineData(0, 0, 0)]
    public void Add_ReturnsExpected(int a, int b, int expected)
        => Assert.Equal(expected, _sut.Add(a, b));

    [Fact]
    public void Divide_ByZero_Throws()
        => Assert.Throws<DivideByZeroException>(() => _sut.Divide(1, 0));

    [Theory]
    [InlineData(10, 2, 5)]
    [InlineData(7, 2, 3.5)]
    public void Divide_ReturnsExpected(int a, int b, double expected)
        => Assert.Equal(expected, _sut.Divide(a, b), 3);
}
