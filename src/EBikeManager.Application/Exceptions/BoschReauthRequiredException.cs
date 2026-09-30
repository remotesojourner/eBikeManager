namespace EBikeManager.Application.Exceptions;

public sealed class BoschReauthRequiredException : Exception
{
    public BoschReauthRequiredException()
    {
    }

    public BoschReauthRequiredException(string message) : base(message)
    {
    }

    public BoschReauthRequiredException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
