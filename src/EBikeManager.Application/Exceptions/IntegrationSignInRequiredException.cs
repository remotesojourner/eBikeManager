namespace EBikeManager.Application.Exceptions;

public sealed class IntegrationSignInRequiredException : Exception
{
    public IntegrationSignInRequiredException()
    {
    }

    public IntegrationSignInRequiredException(string message) : base(message)
    {
    }

    public IntegrationSignInRequiredException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
