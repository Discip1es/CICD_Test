namespace MyApp.Domain.Exceptions;

public sealed class InvalidExpressionException : DomainException
{
    public int Position { get; }

    public InvalidExpressionException(string message, int position = -1)
        : base(position >= 0 ? $"{message} (at position {position})" : message)
    {
        Position = position;
    }
}
