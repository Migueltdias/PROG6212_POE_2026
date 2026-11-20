using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace RaceDay.Api.Data;

public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    toDatabase => toDatabase.Kind == DateTimeKind.Local ? toDatabase.ToUniversalTime() : toDatabase,
    fromDatabase => DateTime.SpecifyKind(fromDatabase, DateTimeKind.Utc));
