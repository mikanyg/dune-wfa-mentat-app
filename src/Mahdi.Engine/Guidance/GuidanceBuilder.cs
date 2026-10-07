using System.Collections.Immutable;
using Mahdi.Engine.Model;
using Mahdi.Engine.Rules;

namespace Mahdi.Engine.Guidance;

/// <summary>
/// Turns the automa state into instructions with this round's names filled in. The texts paraphrase
/// the Mahdi Solo Mode rules (rulebook pp. 37-42) and the FAQ 3.1 clarifications.
/// </summary>
public sealed class GuidanceBuilder(GameContent content)
{
    private const string FirstPossible = "Do the FIRST one that is possible:";

    public static string SectorName(Sector sector) => sector switch
    {
        Sector.NorthWest => "North-West Sector",
        Sector.NorthEast => "North-East Sector",
        Sector.SouthWest => "South-West Sector",
        Sector.SouthEast => "South-East Sector",
        Sector.Central => "Central Sectors (all 4 count as one)",
        _ => sector.ToString(),
    };

    public static string FaceName(DieFace face) => face switch
    {
        DieFace.Leadership => "Leadership",
        DieFace.Strategy => "Strategy",
        DieFace.Deployment => "Deployment",
        DieFace.Mentat => "Mentat",
        DieFace.House => "House",
        _ => face.ToString(),
    };

    public static string DeckName(PlanningDeck deck) =>
        deck == PlanningDeck.Harkonnen ? "House Harkonnen" : "Corrino Ally";

    public string TargetName(GameState state) =>
        state.Tactical.TargetCardId is { } id ? content.Card(id).Sietch : "the nearest Sietch (no Target Sietch)";

    public string HarvestSectorName(GameState state) =>
        state.Tactical.HarvestCardId is { } id ? SectorName(content.Card(id).Sector) : "the Harvesting Sector";

    public string TargetLabel(GameState state) =>
        state.Tactical.TargetCardId is { } id
            ? $"{content.Card(id).Sietch} ({SectorName(content.Card(id).Sector)})"
            : "None";

    public static Guidance Setup() => new(
        "Setup",
        "Set up as for a 2-player game, with these solo changes:",
        null,
        [
            new("Shuffle the 8 Solo Mode Tactical cards into a facedown Tactical deck (the app draws them for you)."),
            new("Leave Sietch tokens and Atreides Deployment tokens facedown. You may inspect them at any time."),
            new("Pool both sets of Harkonnen Starting Deployment tokens; they are placed when a Harkonnen Legion leaves a Settlement."),
            new("Keep the Harkonnen Planning decks (House Harkonnen and Corrino Ally) near the board. The Harkonnens hold no hand of cards."),
            new("Leader cards in play at the start: Baron Harkonnen, Beast Rabban and Captain Aramsham."),
        ],
        ["If you can, you choose: whenever several options meet the Harkonnen criteria, pick the one that hurts the Atreides most."],
        "Rulebook p. 37");

    public Guidance RoundStart(GameState state)
    {
        var steps = ImmutableArray.CreateBuilder<GuidanceStep>();
        steps.Add(new($"Harvesting Sector card: {HarvestSectorName(state)}."));
        steps.Add(new($"Target Sietch card: {TargetLabel(state)}."));
        steps.Add(new("Draw and reveal 2 Prescience cards (not 3)."));
        steps.Add(new(
            $"Add 1 House Harkonnen and 1 Corrino Ally Planning card facedown to the Reinforcements deck (now {state.Reinforcements})."));

        var notes = state.Tactical.Discard.IsEmpty
            ? ImmutableArray<string>.Empty
            : [$"Discarded while drawing the Target: {string.Join(", ", state.Tactical.Discard.Select(id => content.Card(id).Sietch))} (same Sector or Sietch destroyed)."];

        return new Guidance($"Round {state.Round}", "Start of the round", null, steps.ToImmutable(), notes, "Rulebook p. 37");
    }

    public Guidance VehiclePlacement(GameState state)
    {
        var row = content.RowForStep(state.LowestImperiumStep);
        var target = TargetName(state);
        var sector = HarvestSectorName(state);

        return new Guidance(
            "Vehicle Placement",
            $"Place {row.Harvesters} Harvesters, {row.Carryalls} Carryall{Plural(row.Carryalls)} and {row.Ornithopters} Ornithopters. Set aside {state.Dice.SetAside} Harkonnen Action dice.",
            "Place them in this order:",
            [
                new($"Harvesters in the {sector}:",
                [
                    "1. Empty Deep Desert Areas not adjacent to an Atreides Legion or Sietch.",
                    "2. Empty Desert Areas not adjacent to an Atreides Legion or Sietch.",
                    "3. Any remaining free Deep Desert Areas.",
                    "4. Any remaining free Desert Areas.",
                    $"Not enough room? Use an adjacent Sector of your choice (same order), but not the Sector of {target}.",
                ]),
                new("Carryalls in the Air Zones that protect the most Harvesters."),
                new("Ornithopters in unoccupied Air Zones:",
                [
                    "1. In each Air Zone connected to the Sector of a Harkonnen Legion exactly 2 Areas away from a Sietch it could attack.",
                    $"2. Then in Air Zones connected to the Sector of {target}.",
                    $"3. If those are full: Air Zones connecting Sectors adjacent to {target}, Central-to-Central Air Zones first.",
                ]),
            ],
            [
                "Ornithopters cannot be placed in an Air Zone holding a Carryall (FAQ).",
                "Ornithopters cannot be used for Scouting when playing solo.",
            ],
            "Rulebook p. 38");
    }

    public Guidance Turn(GameState state)
    {
        var turn = state.Dice.Pending ?? throw new InvalidOperationException("No Harkonnen turn in progress.");
        var regular = Regular(state, turn);

        if (turn.SpecialLeader is not { } leader)
        {
            return regular;
        }

        return Special(state, turn, content.Leader(leader)) with { Alternative = regular };
    }

    public static Guidance DesertHazards() => new(
        "Desert Hazards",
        "Play this phase as usual.",
        "Apply Coriolis Storm Hits to Harkonnen Legions in this order:",
        HitPriority(),
        ["Harkonnen Legions retreat when a Sandworm appears in their Area (this is not a battle) (FAQ)."],
        "Rulebook pp. 38, 41");

    public static Guidance SpiceResult(SpiceResult result)
    {
        var steps = result.Markers.Select(m => new GuidanceStep(MarkerText(m))).ToList();
        if (result.SupremacyGained)
        {
            steps.Add(new("All Imperium markers were already on the top step: the Harkonnens score 1 Supremacy point instead."));
        }

        var notes = ImmutableArray.CreateBuilder<string>();
        notes.Add(result.NewReserve > 0
            ? "Put the Spice Reserve token on The Spice Must Flow board (1 spice saved)."
            : "Spice Reserve: none.");
        if (result.Wasted > 0)
        {
            notes.Add($"{result.Wasted} spice could not be used and is lost.");
        }

        notes.Add("Carryalls: always save Harvesters in Deep Desert Areas first.");
        notes.Add("Stockpiling is not used in solo.");

        var total = result.Collected + result.ReserveUsed;
        return new Guidance(
            "Spice Harvesting",
            $"{total} spice to spend ({result.Collected} harvested{(result.ReserveUsed > 0 ? $" + {result.ReserveUsed} from the Reserve" : "")}).",
            "Move the Imperium markers:",
            [.. steps],
            notes.ToImmutable(),
            "Rulebook p. 38");
    }

    public Guidance EndOfRound(GameState state) => new(
        "End of the Round",
        $"Apply the usual steps, then the Harkonnen Supremacy marker advances 1 step (to {Math.Min(content.Supremacy.Max, state.Supremacy + 1)}).",
        null,
        [
            new("Atreides: check End of the Round Prescience cards and your Secret Objective. If it is met, declare victory."),
            new("Remove all Ornithopters and Carryalls from the board."),
            new("Never discard Planning cards in the Reinforcements deck, and never replace Harkonnen Named Leaders on the board."),
            new("The 8 Tactical cards are reshuffled into a new deck (the app does this)."),
        ],
        [],
        "Rulebook p. 39");

    public static Guidance Combat(GameState state) => new(
        "Harkonnen battle",
        state.IsBanActive(ImperiumPower.Landsraad)
            ? "Landsraad Ban: no Reinforcements cards may be discarded for Combat dice."
            : $"Each battle round, discard Reinforcements cards until the Harkonnens roll 6 Combat dice ({state.Reinforcements} available).",
        "Apply Hits to a Harkonnen Legion in this order:",
        HitPriority(),
        [
            "Harkonnen Legions never retreat during a battle. They stop attacking only if, at the start of a Combat round, their Combat Power is half or less of the opposing Legion's (count individual Units).",
            "When attacking a Sietch, the Harkonnens do not need to take 1 Hit to continue the battle.",
            "If the Atreides retreat, you choose where, but empty Areas come first.",
            "Victorious Legions may advance into the attacked Area or stay where they are (FAQ).",
            CombatPowerNote,
        ],
        "Rulebook p. 41");

    private const string UnitAvailabilityNote =
        "If a Unit type is not available, use the type with the next higher Combat Power (or the next lower if none).";

    private const string CombatPowerNote =
        "Combat Power: 1 per Unit, 2 per Deployment token, +1 per Leader. For ties or single Units: Generic Leader 1, Regular Unit or Named Leader 2, Elite 3, Sardaukar or Fedaykin 4. Sietch rank does not count.";

    private Guidance Regular(GameState state, PendingTurn turn) => turn.Face switch
    {
        DieFace.Leadership => Military(state, "Leadership", true),
        DieFace.Strategy => Military(state, "Strategy", false),
        DieFace.Deployment => Deployment(state, turn, "3 Regular Units", "Deployment"),
        DieFace.Mentat => Mentat(state, turn),
        DieFace.House => House(state),
        _ => throw new ArgumentOutOfRangeException(nameof(turn), turn.Face, null),
    };

    private Guidance Special(GameState state, PendingTurn turn, LeaderDefinition leader) => leader.Id switch
    {
        LeaderId.BeastRabban => new Guidance(
            "Leadership: Beast Rabban",
            "Move the Legion containing Beast Rabban toward " + TargetName(state) + ", then move it again to an adjacent Area.",
            "Follow the Harkonnen Movement criteria:",
            MovementSteps(state),
            [$"{leader.Name}'s card is spent.", "Rabban must take part in each move (FAQ)."],
            "Rulebook pp. 39-41"),
        LeaderId.FeydRautha => Military(state, "Leadership: Feyd-Rautha", true) with
        {
            Headline = "Move and attack with the Legion containing Feyd-Rautha.",
            Notes = [$"{leader.Name}'s card is spent.", "Feyd-Rautha's Legion moves first if that lets it attack.", CombatPowerNote],
        },
        LeaderId.CaptainAramsham => Deployment(
            state, turn, "2 Regular Units, 1 Sardaukar Unit", "Deployment: Captain Aramsham", $"{leader.Name}'s card is spent."),
        LeaderId.ThufirHawat or LeaderId.GaiusHelenMohiam => Mentat(state, turn) with
        {
            Title = $"Mentat: {leader.Name}",
            Headline = $"Draw 3 {DeckName(leader.Deck)} Planning cards and play them immediately, one at a time.",
            Notes = [$"{leader.Name}'s card is spent (solo variant of the special action)."],
        },
        // The special replaces the regular House Action, so no Vehicles are placed.
        LeaderId.BaronHarkonnen => new Guidance(
            "House: Baron Harkonnen",
            "Replace 3 Regular Units on the board with 3 Elite Units. No Vehicles are placed.",
            "Choose the Legions in this priority:",
            ReplacePriority(state),
            [$"{leader.Name}'s card is spent (the special action replaces the regular House Action).", UnitAvailabilityNote],
            "Rulebook pp. 39-40"),
        LeaderId.ShaddamIV => new Guidance(
            "Strategy: Emperor Shaddam IV",
            "Replace 3 Elite Units on the board with 3 Sardaukar Units.",
            "Choose the Legions in this priority:",
            ReplacePriority(state),
            [$"{leader.Name}'s card is spent."],
            "Rulebook p. 40"),
        _ => new Guidance(leader.Name, leader.Special, null, [], [], null),
    };

    private Guidance Military(GameState state, string title, bool leaderRequired)
    {
        var target = TargetName(state);
        var withLeader = leaderRequired ? " with a Leader" : string.Empty;
        var attackSietch = leaderRequired ? "Make a Surprise Attack on a Sietch" : "Attack a Sietch";
        var attackLegion = leaderRequired ? "Make a Surprise Attack on an adjacent Atreides Legion" : "Attack an adjacent Atreides Legion";

        var notes = ImmutableArray.CreateBuilder<string>();
        if (leaderRequired)
        {
            notes.Add("Leadership: only Legions containing at least 1 Leader count, for attacking and moving (FAQ).");
        }

        notes.Add("An attacking Harkonnen Legion must have a greater Combat Power than the defending Atreides Legion.");
        notes.Add(CombatPowerNote);
        AddBanNotes(state, notes);

        return new Guidance(
            title,
            leaderRequired ? "Attack or move with Legions that have a Leader." : "Attack with a Legion, or move 2 Legions.",
            FirstPossible,
            [
                new($"{attackSietch} (any Sietch) with the nearest Legion{withLeader} that out-powers the defenders. Use an Ornithopter only if necessary.",
                [
                    "1. The Sietch with the highest rank (even if unrevealed).",
                    "2. The Legion with the greatest Combat Power difference over the defenders.",
                    "3. A Legion that does not need an Ornithopter.",
                    $"4. The Target Sietch ({target}).",
                ]),
                new($"{attackLegion} with a Legion{withLeader} that out-powers it.",
                [
                    "1. The Atreides Legion with the highest Combat Power.",
                    "2. The Atreides Legion containing a Named Leader.",
                    "Ornithopters cannot be used to attack Legions when playing solo.",
                ]),
                new($"Move 2 different Legions{withLeader} toward {target}.", MovementDetails(state)),
            ],
            notes.ToImmutable(),
            "Rulebook pp. 39-41");
    }

    private ImmutableArray<GuidanceStep> MovementSteps(GameState state) =>
        [.. MovementDetails(state).Select(d => new GuidanceStep(d))];

    private ImmutableArray<string> MovementDetails(GameState state)
    {
        var target = TargetName(state);
        return
        [
            $"Move one Legion at a time, starting with the one closest to {target}; at equal distance, the highest Combat Power first.",
            "Take the shortest path (fewest free Areas to cross), using an Ornithopter if available. Never more than 1 Ornithopter per turn.",
            $"The moving Legion must out-power any Atreides Legion defending {target}. If none can, use a temporary target for this turn: the Sietch closest to {target}, then the Sietch with the highest rank.",
            $"Do not move Legions adjacent to {target} unless they can merge with another adjacent Harkonnen Legion into the strongest Legion closest to {target}.",
            "Path ties: end in an Area with a Harkonnen Legion below its stacking limit (move the strongest Units plus all Leaders), then closest to a Sietch, then Mountain, then Plateau or Minor Erg, then Desert or Deep Desert without Wormsign.",
            "Harkonnens ignore impassable borders in solo.",
            "A Legion leaving a Settlement Area leaves 2 Harkonnen Deployment tokens there (1 black, 1 silver, facedown).",
        ];
    }

    private Guidance Deployment(GameState state, PendingTurn turn, string units, string title, string? leaderNote = null)
    {
        var leader = turn.DeployLeader is { } id ? content.Leader(id).Name : "1 Bashar Leader";
        var notes = ImmutableArray.CreateBuilder<string>();
        if (leaderNote is not null)
        {
            notes.Add(leaderNote);
        }

        var others = LeaderRules.DeployCandidates(content, state).Where(l => l != turn.DeployLeader).ToList();
        if (turn.DeployLeader is not null && others.Count > 0 && !content.Leader(turn.DeployLeader.Value).DeployFirst)
        {
            notes.Add($"If you can, you choose: {string.Join(", ", others.Select(l => content.Leader(l).Name))} could be deployed instead.");
        }

        notes.Add("Respect the stacking limit: deploy any excess Units in another Settlement, using the same priority.");
        notes.Add(UnitAvailabilityNote);
        AddBanNotes(state, notes);

        return new Guidance(
            title,
            $"Deploy {units} and {leader} in the same Harkonnen Settlement.",
            "Choose the Settlement in this priority:",
            [
                new("1. The Settlement containing the Legion with the highest Combat Power."),
                new($"2. The Settlement closest to {TargetName(state)}."),
            ],
            notes.ToImmutable(),
            "Rulebook pp. 39-40");
    }

    private Guidance Mentat(GameState state, PendingTurn turn)
    {
        var first = DeckName(turn.StartDeck);
        var second = DeckName(GameReducer.Other(turn.StartDeck));
        return new Guidance(
            "Mentat",
            $"Draw and immediately play 2 Planning cards: the first from {first}, the second from {second}.",
            "Resolve each card like this:",
            [
                new("Deploy, move or attack: use the Deployment or Leadership and Strategy criteria."),
                new("Place or replace Units: use the House criteria."),
                new("Place Vehicles: use the Vehicle Placement rules."),
                new("Draw cards: draw them alternately and put them on the Reinforcements deck (More events: Add Reinforcements)."),
                new("Play a card: draw and play the next card in the alternation."),
                new($"Any other effect: make it happen as close as possible to, or toward, {TargetName(state)}."),
            ],
            [
                "If part of a card cannot be resolved, the card has no effect and goes onto the Reinforcements deck instead (More events: Add Reinforcements).",
                "Hawat's Scheming: the first copy goes next to the board; when a second is played, discard both and apply the effect.",
            ],
            "Rulebook p. 40");
    }

    private Guidance House(GameState state)
    {
        var notes = ImmutableArray.CreateBuilder<string>();
        AddBanNotes(state, notes);
        return new Guidance(
            "House",
            "Apply both effects, top one first.",
            "In order:",
            [
                new("Replace 2 Regular Units with 2 Elite Units, choosing Legions in this priority:",
                    [.. ReplacePriority(state).Select(s => s.Text)]),
                new($"Place 1 Harvester and 1 Ornithopter using the Vehicle Placement rules (Harvester in {HarvestSectorName(state)})."),
            ],
            notes.ToImmutable(),
            "Rulebook p. 40");
    }

    private ImmutableArray<GuidanceStep> ReplacePriority(GameState state) =>
    [
        new("1. The Harkonnen Legion(s) closest to a Sietch."),
        new("2. The Legion(s) with the highest Combat Power relative to the Atreides Legion defending that Sietch."),
        new($"3. The Legion closest to {TargetName(state)}."),
    ];

    private static ImmutableArray<GuidanceStep> HitPriority() =>
    [
        new("1. Eliminate Leaders, Bashar Leaders first, until only 1 Leader remains (a Named one if possible)."),
        new("2. Replace Elite Units with Regular Units."),
        new("3. Replace Sardaukar Units with Regular Units."),
        new("4. Eliminate Regular Units. If the Hits would remove the last Regular Unit while a Leader remains, eliminate the Leader first."),
    ];

    private void AddBanNotes(GameState state, ImmutableArray<string>.Builder notes)
    {
        foreach (var ban in content.Bans.Where(b => state.IsBanActive(b.Power)))
        {
            notes.Add($"{ban.Name} Ban: {ban.SoloEffect}");
        }
    }

    private static string MarkerText(MarkerOutcome m)
    {
        var name = m.Power switch
        {
            ImperiumPower.Choam => "CHOAM",
            ImperiumPower.SpacingGuild => "Spacing Guild",
            ImperiumPower.Landsraad => "Landsraad",
            _ => m.Power.ToString(),
        };

        return m.Change switch
        {
            MarkerChange.Raised => $"{name}: spend 3, raise to step {m.To}.",
            MarkerChange.Kept => $"{name}: spend 2, stays on step {m.To}.",
            MarkerChange.Dropped => $"{name}: no spice, drops to step {m.To}. Its Ban is activated.",
            MarkerChange.StayedAtBottom => $"{name}: no spice, stays on the bottom step. Its Ban stays active.",
            _ => name,
        };
    }

    private static string Plural(int count) => count == 1 ? string.Empty : "s";
}
