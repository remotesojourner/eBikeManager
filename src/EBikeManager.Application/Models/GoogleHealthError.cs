namespace EBikeManager.Application.Models;

public sealed record GoogleHealthError(int Code, string Message)
{
    public const int NotFound = 5;
    public const int AlreadyExists = 6;
}
