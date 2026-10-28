using System.Collections.Immutable;
using Mahdi.Engine.Model;
using Mahdi.Engine.Rules;

namespace Mahdi.Engine.Guidance;

/// <summary>Coriolis Storm and Sandworm Hits (rulebook pp. 20, 31 and 41) as shown in the in-app help sheet.</summary>
/// <param name="ExampleSwords">Swords rolled in the worked example.</param>
/// <param name="ExampleSpecials">Specials rolled in the worked example.</param>
/// <param name="ExampleSteps">How the example's Hits are applied, in the Harkonnen order.</param>
public sealed record DesertHazardHelp(
    ImmutableArray<HazardAttackRow> Attacks,
    ImmutableArray<HazardDieRow> DieResults,
    ImmutableArray<HazardTerrainRow> SpecialHits,
    ImmutableArray<string> ExposedAreas,
    ImmutableArray<string> NotExposed,
    string ExampleLegion,
    HazardTerrain ExampleTerrain,
    int ExampleSwords,
    int ExampleSpecials,
    ImmutableArray<string> ExampleSteps,
    ImmutableArray<string> HitPriority,
    ImmutableArray<string> Notes,
    string RuleReference)
{
    public int ExampleHits => HazardRules.Hits(ExampleTerrain, ExampleSwords, ExampleSpecials);

    public static DesertHazardHelp Rules { get; } = new(
        [
            new(HazardAttack.CoriolisStorm, "Coriolis Storm", HazardRules.Dice(HazardAttack.CoriolisStorm),
                "Every Harkonnen Legion in an exposed Area"),
            new(HazardAttack.Sandworm, "Sandworm", HazardRules.Dice(HazardAttack.Sandworm),
                "A Harkonnen Legion that cannot retreat from the Sandworm"),
            new(HazardAttack.WildMaker, "Wild Maker", HazardRules.Dice(HazardAttack.WildMaker),
                "As a Sandworm, when the Wild Maker is placed instead"),
        ],
        [
            new(CombatDieFace.Sword, "Sword", "1 Hit, on any terrain"),
            new(CombatDieFace.Special, "Special (star)", "Hits by terrain, see below"),
            new(CombatDieFace.Shield, "Shield", "No effect: there is no Hit to cancel"),
        ],
        [
            new(HazardTerrain.DeepDesert, "Deep Desert", HazardRules.SpecialHits(HazardTerrain.DeepDesert)),
            new(HazardTerrain.Desert, "Desert", HazardRules.SpecialHits(HazardTerrain.Desert)),
            new(HazardTerrain.MinorErgOrPlateau, "Minor Erg or Plateau", HazardRules.SpecialHits(HazardTerrain.MinorErgOrPlateau)),
        ],
        ["Plateau", "Minor Erg", "Desert", "Deep Desert"],
        [
            "Mountains",
            "The 5 central Plateau Areas encircled by Mountains (storms only)",
            "Atreides Legions are never hit by Coriolis Storms",
        ],
        "A Harkonnen Legion in Deep Desert with Feyd-Rautha, 1 Bashar, 1 Elite, 1 Sardaukar and 2 Regular Units",
        HazardTerrain.DeepDesert,
        1,
        1,
        [
            "Remove the Bashar: Feyd-Rautha stays as the last Leader.",
            "Replace the Elite Unit with a Regular Unit.",
            "Replace the Sardaukar Unit with a Regular Unit.",
        ],
        [.. GuidanceBuilder.HitPriority().Select(step => step.Text)],
        [
            "If a Sandworm destroys a Legion, or the Legion retreats, a Harvester in the same Area is removed too, unless a Carryall saves it (only during Desert Hazards).",
            "A Sandworm can strike a Legion in Desert, Deep Desert, Minor Erg or Plateau, never in a Mountain Area.",
            "Remove casualties at once. A Named Leader eliminated this way goes to the Regeneration Tank.",
        ],
        "Rulebook pp. 20, 31 and 41");
}

public sealed record HazardAttackRow(HazardAttack Attack, string Name, int Dice, string WhoIsHit);

public sealed record HazardDieRow(CombatDieFace Face, string Name, string Effect);

public sealed record HazardTerrainRow(HazardTerrain Terrain, string Label, int Hits);
