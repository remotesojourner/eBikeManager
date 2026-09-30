using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EBikeManager.Application.Data.Converters;

internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter() : base(
        value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value,
        stored => DateTime.SpecifyKind(stored, DateTimeKind.Utc))
    {
    }
}
