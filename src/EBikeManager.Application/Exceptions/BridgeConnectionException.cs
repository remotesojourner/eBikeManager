namespace EBikeManager.Application.Exceptions;

public sealed class BridgeConnectionException : Exception
{
    public BridgeConnectionException()
    {
    }

    public BridgeConnectionException(string message) : base(message)
    {
    }

    public BridgeConnectionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
