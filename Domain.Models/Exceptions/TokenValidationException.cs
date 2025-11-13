namespace Domain.Models.Exceptions;
public class TokenValidationException : Exception
{
    public int StatusCode { get; }
    public TokenValidationException(string message, int statusCode = 401)
        : base(message) => StatusCode = statusCode;

    public TokenValidationException() : base()
    {
    }

    public TokenValidationException(string? message) : base(message)
    {
    }

    public TokenValidationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
