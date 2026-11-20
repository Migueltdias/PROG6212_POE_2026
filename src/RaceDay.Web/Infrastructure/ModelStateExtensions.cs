using System.Reflection;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using RaceDay.Web.ApiClient;

namespace RaceDay.Web.Infrastructure;

public static class ModelStateExtensions
{
    public static void AddApiErrors(this ModelStateDictionary modelState, ApiResult result, Type? modelType = null,
        IReadOnlyDictionary<string, string>? keyMap = null, string prefix = "")
    {
        if (result.FieldErrors.Count == 0)
        {
            modelState.AddModelError(string.Empty, result.ErrorMessage);
            return;
        }

        foreach (var (apiKey, messages) in result.FieldErrors)
        {
            var key = MapKey(apiKey, keyMap);
            var target = modelType is not null && HasProperty(modelType, key) ? prefix + key : string.Empty;
            foreach (var message in messages)
            {
                modelState.AddModelError(target, message);
            }
        }

        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            modelState.AddModelError(string.Empty, result.Error);
        }
    }

    private static string MapKey(string apiKey, IReadOnlyDictionary<string, string>? keyMap)
    {
        if (keyMap is not null)
        {
            foreach (var (from, to) in keyMap)
            {
                if (apiKey.StartsWith(from, StringComparison.OrdinalIgnoreCase))
                {
                    return to + PascalCase(apiKey[from.Length..]);
                }
            }
        }

        return PascalCase(apiKey);
    }

    private static string PascalCase(string path) =>
        string.Join('.', path.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => segment.Length == 0 ? segment : char.ToUpperInvariant(segment[0]) + segment[1..]));

    private static bool HasProperty(Type modelType, string key)
    {
        var first = key.Split('.', '[')[0];
        return first.Length > 0
               && modelType.GetProperty(first, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase) is not null;
    }
}
