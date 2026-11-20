using System.Text.Json;

namespace RaceDay.Api.Infrastructure;

public static class JsonName
{
    public static string Of(string propertyName) => JsonNamingPolicy.CamelCase.ConvertName(propertyName);
}
