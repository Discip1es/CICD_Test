namespace MyApp.Domain.Exceptions;

public sealed class DivisionByZeroException : DomainException
{
    public DivisionByZeroException()
        : base("Cannot divide by zero.")
    {
    }

    public DivisionByZeroException(string dividend, string divisor)
        : base($"Cannot divide {dividend} by zero.")
    {
    }
}
