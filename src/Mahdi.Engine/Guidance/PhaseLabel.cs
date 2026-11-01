using Mahdi.Engine.Model;

namespace Mahdi.Engine.Guidance;

/// <summary>Who plays a phase of the round alone; most phases involve both sides.</summary>
public enum PhaseOwner
{
    Both,
    Harkonnen,
    Atreides,
}

/// <summary>
/// The name of a phase as the rulebook numbers it: Start of Round, Phases 1-4, End of Round.
/// For example "Phase 1: Vehicle Placement (Harkonnen)".
/// </summary>
public sealed record PhaseLabel(Phase Phase, int? Number, string Name, PhaseOwner Owner)
{
    /// <summary>"Phase 1: Vehicle Placement", or just the name for phases without a number.</summary>
    public string Title => Number is { } n ? $"Phase {n}: {Name}" : Name;

    /// <summary>The title with the owner, e.g. "Phase 1: Vehicle Placement (Harkonnen)".</summary>
    public string FullTitle => Owner == PhaseOwner.Both ? Title : $"{Title} ({Owner})";

    public static PhaseLabel For(Phase phase) => phase switch
    {
        Phase.Setup => new(phase, null, "Setup", PhaseOwner.Both),
        Phase.RoundStart => new(phase, null, "Start of Round", PhaseOwner.Both),
        Phase.VehiclePlacement => new(phase, 1, "Vehicle Placement", PhaseOwner.Harkonnen),
        Phase.ActionResolution => new(phase, 2, "Action Resolution", PhaseOwner.Both),
        Phase.DesertHazards => new(phase, 3, "Desert Hazards", PhaseOwner.Atreides),
        Phase.SpiceHarvesting => new(phase, 4, "Spice Harvesting", PhaseOwner.Harkonnen),
        Phase.EndOfRound => new(phase, null, "End of Round", PhaseOwner.Both),
        Phase.GameOver => new(phase, null, "Game Over", PhaseOwner.Both),
        _ => new(phase, null, phase.ToString(), PhaseOwner.Both),
    };
}
