using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace UserService.Infrastructure.JsonConverters;

/// <summary>
/// Serializes enum values as SCREAMING_SNAKE_CASE strings (e.g. CheckedOut → "CHECKED_OUT").
/// Deserializes both SCREAMING_SNAKE_CASE and PascalCase inputs back to the enum.
/// </summary>
public sealed class ScreamingSnakeEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var converterType = typeof(ScreamingSnakeEnumConverter<>).MakeGenericType(typeToConvert);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

public sealed class ScreamingSnakeEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    // Matches an uppercase letter immediately preceded by a lowercase letter or digit.
    // "CheckedOut" → "Checked_Out" → "CHECKED_OUT"
    // "Patron"     → "Patron"      → "PATRON"
    private static readonly Regex PascalToSnakeRegex =
        new Regex("(?<=[a-z0-9])([A-Z])", RegexOptions.Compiled);

    private static string ToScreamingSnake(string pascalCase) =>
        PascalToSnakeRegex.Replace(pascalCase, "_$1").ToUpperInvariant();

    // "CHECKED_OUT" → "CHECKEDOUT" → matched by Enum.TryParse ignoreCase
    private static string FromScreamingSnake(string screamingSnake) =>
        screamingSnake.Replace("_", string.Empty);

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString() ?? string.Empty;
        var normalized = FromScreamingSnake(raw);

        if (Enum.TryParse<T>(normalized, ignoreCase: true, out var result))
            return result;

        throw new JsonException(
            $"Unable to convert \"{raw}\" to enum type {typeof(T).Name}.");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        writer.WriteStringValue(ToScreamingSnake(value.ToString()));
}
