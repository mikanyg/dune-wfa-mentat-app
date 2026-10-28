using Mahdi.Engine.Model;

namespace Mahdi.Engine.Rules;

/// <summary>Coriolis Storm and Sandworm attacks on Harkonnen Legions (rulebook pp. 20 and 31).</summary>
public static class HazardRules
{
    /// <summary>At most this many Wormsign tokens bring Sandworms in one Desert Hazards phase (one more with the Wild Maker).</summary>
    public const int MaxSandworms = 4;

    public static int Dice(HazardAttack attack) => attack switch
    {
        HazardAttack.CoriolisStorm => 2,
        HazardAttack.Sandworm => 4,
        HazardAttack.WildMaker => 6,
        _ => throw new ArgumentOutOfRangeException(nameof(attack), attack, null),
    };

    /// <summary>Hits scored by each Special result on this terrain.</summary>
    public static int SpecialHits(HazardTerrain terrain) => terrain switch
    {
        HazardTerrain.DeepDesert => 2,
        HazardTerrain.Desert => 1,
        HazardTerrain.MinorErgOrPlateau => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(terrain), terrain, null),
    };

    /// <summary>Each Sword is 1 Hit and each Special scores the terrain's Hits; Shields have no effect.</summary>
    public static int Hits(HazardTerrain terrain, int swords, int specials) =>
        swords + specials * SpecialHits(terrain);
}
