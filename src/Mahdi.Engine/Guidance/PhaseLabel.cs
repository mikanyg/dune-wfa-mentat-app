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
/// For example "Phase 1: Vehicle Placement (Harkonnen)", short "1: Vehicles", badge "1".
/// </summary>
/// <param name="ShortName">One word for narrow screens, e.g. "Vehicles".</param>
/// <param name="Badge">The stepper badge: S, 1-4 or E (null outside the round).</param>
public sealed record PhaseLabel(Phase Phase, int? Number, string Name, PhaseOwner Owner, string ShortName, string? Badge)
{
    /// <summary>The phases of a round in order, as the stepper shows them.</summary>
    public static readonly IReadOnlyList<Phase> RoundPhases =
        [Phase.RoundStart, Phase.VehiclePlacement, Phase.ActionResolution, Phase.DesertHazards, Phase.SpiceHarvesting, Phase.EndOfRound];

    /// <summary>"Phase 1: Vehicle Placement", or just the name for phases without a number.</summary>
    public string Title => Number is { } n ? $"Phase {n}: {Name}" : Name;

    /// <summary>The title with the owner, e.g. "Phase 1: Vehicle Placement (Harkonnen)".</summary>
    public string FullTitle => Owner == PhaseOwner.Both ? Title : $"{Title} ({Owner})";

    /// <summary>"1: Vehicles", or the short name for phases without a number ("Start Round").</summary>
    public string ShortTitle => Number is { } n ? $"{n}: {ShortName}" : ShortName;

    public static PhaseLabel For(Phase phase) => phase switch
    {
        Phase.Setup => new(phase, null, "Setup", PhaseOwner.Both, "Setup", null),
        Phase.RoundStart => new(phase, null, "Start of Round", PhaseOwner.Both, "Start Round", "S"),
        Phase.VehiclePlacement => new(phase, 1, "Vehicle Placement", PhaseOwner.Harkonnen, "Vehicles", "1"),
        Phase.ActionResolution => new(phase, 2, "Action Resolution", PhaseOwner.Both, "Actions", "2"),
        Phase.DesertHazards => new(phase, 3, "Desert Hazards", PhaseOwner.Atreides, "Hazards", "3"),
        Phase.SpiceHarvesting => new(phase, 4, "Spice Harvesting", PhaseOwner.Harkonnen, "Harvest", "4"),
        Phase.EndOfRound => new(phase, null, "End of Round", PhaseOwner.Both, "End Round", "E"),
        Phase.GameOver => new(phase, null, "Game Over", PhaseOwner.Both, "Game Over", null),
        _ => new(phase, null, phase.ToString(), PhaseOwner.Both, phase.ToString(), null),
    };
}
