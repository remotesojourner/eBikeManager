namespace EBikeManager.Application.Models;

public sealed record BoschPassFile(string FileId, string FileType, DateTime? CreatedAt, DateTime? UpdatedAt);
