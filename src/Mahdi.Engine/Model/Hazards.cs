namespace Mahdi.Engine.Model;

/// <summary>Results on a Combat die.</summary>
public enum CombatDieFace
{
    /// <summary>1 Hit.</summary>
    Sword,

    /// <summary>Cancels 1 opposing Hit in battle.</summary>
    Shield,

    /// <summary>The star result: Hits that depend on the terrain (storms, Sandworms) or on Leaders (battles).</summary>
    Special,
}

/// <summary>What strikes a Harkonnen Legion during Desert Hazards.</summary>
public enum HazardAttack
{
    CoriolisStorm,
    Sandworm,
    WildMaker,
}

/// <summary>Terrain types that Coriolis Storms and Sandworms can strike.</summary>
public enum HazardTerrain
{
    DeepDesert,
    Desert,
    MinorErgOrPlateau,
}
