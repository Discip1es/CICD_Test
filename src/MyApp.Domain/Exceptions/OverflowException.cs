namespace MyApp.Domain.Exceptions;

public sealed class OverflowException : DomainException
{
    public OverflowException(string operation)
        : base($"Arithmetic overflow while performing '{operation}'. The result is outside the supported range.")
    {
    }
}
