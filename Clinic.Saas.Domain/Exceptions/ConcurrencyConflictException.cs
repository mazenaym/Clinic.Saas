namespace Clinic.Saas.Domain.Exceptions;

public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message) : base(message)
    {
    }
}
