using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mahdi.Engine.Model;

namespace Mahdi.Engine.Content;

/// <summary>Loads the game data embedded in the engine assembly (Content/*.json).</summary>
public static class ContentLoader
{
    private static readonly Lazy<GameContent> Embedded = new(LoadEmbedded);

    public static GameContent Default => Embedded.Value;

    private static GameContent LoadEmbedded()
    {
        var context = ContentJsonContext.Default;
        return new GameContent(
            Read("dice.json", context.DiceContent),
            Read("spice-must-flow.json", context.ImmutableArraySpiceMustFlowRow),
            Read("supremacy-track.json", context.SupremacyTrack),
            Read("tactical-cards.json", context.ImmutableArrayTacticalCard),
            Read("leaders.json", context.ImmutableArrayLeaderDefinition),
            Read("bans.json", context.ImmutableArrayBanDefinition));
    }

    private static T Read<T>(string file, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
    {
        var name = $"Mahdi.Engine.Content.{file}";
        using var stream = typeof(ContentLoader).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Missing embedded content '{name}'.");
        return JsonSerializer.Deserialize(stream, typeInfo)
            ?? throw new InvalidOperationException($"Content '{name}' is empty.");
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, UseStringEnumConverter = true)]
[JsonSerializable(typeof(DiceContent))]
[JsonSerializable(typeof(ImmutableArray<SpiceMustFlowRow>))]
[JsonSerializable(typeof(SupremacyTrack))]
[JsonSerializable(typeof(ImmutableArray<TacticalCard>))]
[JsonSerializable(typeof(ImmutableArray<LeaderDefinition>))]
[JsonSerializable(typeof(ImmutableArray<BanDefinition>))]
internal sealed partial class ContentJsonContext : JsonSerializerContext;
