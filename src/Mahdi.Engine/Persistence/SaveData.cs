using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mahdi.Engine.Events;
using Mahdi.Engine.Model;

namespace Mahdi.Engine.Persistence;

/// <summary>
/// A saved game: the seed plus the event history, grouped per command so undo removes one command.
/// </summary>
public sealed record SaveData(
    int Version,
    DiceMode DiceMode,
    int Seed,
    ImmutableArray<ImmutableArray<GameEvent>> History)
{
    public const int CurrentVersion = 1;

    public string ToJson() => JsonSerializer.Serialize(this, EngineJsonContext.Default.SaveData);

    public static SaveData FromJson(string json) =>
        JsonSerializer.Deserialize(json, EngineJsonContext.Default.SaveData)
        ?? throw new JsonException("Empty save data.");
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(SaveData))]
internal sealed partial class EngineJsonContext : JsonSerializerContext;
