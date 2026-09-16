using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ReservationService.Infrastructure.JsonConverters;

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
    private static readonly Regex PascalToSnakeRegex =
        new Regex("(?<=[a-z0-9])([A-Z])", RegexOptions.Compiled);

    private static string ToScreamingSnake(string pascalCase) =>
        PascalToSnakeRegex.Replace(pascalCase, "_$1").ToUpperInvariant();

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