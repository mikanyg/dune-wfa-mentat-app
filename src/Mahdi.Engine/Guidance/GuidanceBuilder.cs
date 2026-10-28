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

    /// <summary>
    /// The Desert Hazards phase (rulebook pp. 20, 30-31) with the solo criteria (pp. 38, 41). Without a game
    /// in progress the round-specific names are left out.
    /// </summary>
    public DesertHazardsGuidance DesertHazards(GameState? state)
    {
        var harvesters = state?.Tactical.HarvestCardId is not null
            ? $" This round's Harvesters went to the {HarvestSectorName(state)}."
            : string.Empty;
        var carryalls = state is not null && content.RowForStep(state.LowestImperiumStep).Carryalls is var count and > 0
            ? $" {count} Carryall{Plural(count)} {(count == 1 ? "was" : "were")} placed this round."
            : string.Empty;

        return new DesertHazardsGuidance(
            [
                new HazardSection(
                    "Wormsigns",
                    "Wormsigns",
                    [
                        $"Place 1 facedown Wormsign token in each Desert or Deep Desert Area with a Harkonnen Legion or Harvester and no Wormsign or Sandworm.{harvesters}",
                        "Then flip all Wormsign tokens on the board and resolve them in any order.",
                    ],
                    [
                        "First discard the Wormsign tokens in Areas with an Atreides Legion or a Sandworm, shuffling them back into the pool.",
                        "Sand: a false alarm. Discard the token.",
                        "Sandworm: place a Sandworm in the Area, then discard the token.",
                        "Burrowing Sandworm: place a Sandworm only if the Area is Deep Desert. Discard the token either way.",
                        $"At most {HazardRules.MaxSandworms} Wormsign tokens bring Sandworms ({HazardRules.MaxSandworms + 1} with the Wild Maker in play). You choose and discard the excess.",
                        "Each Sandworm comes from off the board or is moved from elsewhere on the board. The Wild Maker may be placed instead of a normal Sandworm (FAQ).",
                    ]),
                new HazardSection(
                    "Sandworms",
                    "Sandworms appear",
                    [
                        $"A Harvester alone in the Area is removed, unless a Carryall saves it: remove a Carryall from a connected Air Zone instead.{carryalls} Save Harvesters in Deep Desert first.",
                        $"A Harkonnen Legion must retreat to an adjacent Area you choose, never one with a Wormsign token. If it cannot, the Sandworm attacks it with {HazardRules.Dice(HazardAttack.Sandworm)} Combat dice.",
                    ],
                    [
                        "If the Legion retreats or is destroyed, a Harvester in the same Area is removed too, unless a Carryall saves it.",
                        "A saved Harvester stays in the Area with the Sandworm until it is removed in Spice Harvesting.",
                        $"The Wild Maker attacks with {HazardRules.Dice(HazardAttack.WildMaker)} Combat dice.",
                        "This retreat is not a battle, so the rule that the Harkonnens never retreat does not apply (FAQ).",
                        "Carryalls save Harvesters only in this phase, not when a Sandworm strikes during Action Resolution.",
                    ],
                    HazardAttack.Sandworm),
                new HazardSection(
                    "CoriolisStorms",
                    "Coriolis Storms",
                    [
                        $"Roll {HazardRules.Dice(HazardAttack.CoriolisStorm)} Combat dice for each Harkonnen Legion in a Plateau, Minor Erg, Desert or Deep Desert Area, except the 5 central Plateaus encircled by Mountains. Atreides Legions are not affected.",
                        "Apply the Hits: Leaders (keep 1), then Elite to Regular, then Sardaukar to Regular, then Regular Units.",
                    ],
                    [.. HitPriority().Select(step => step.Text)],
                    HazardAttack.CoriolisStorm),
            ],
            [
                "Remove casualties at once. A Named Leader eliminated by a Sandworm or a storm goes to the Regeneration Tank: tap it in the Leaders panel.",
            ],
            "Rulebook pp. 20, 30-31, 38 and 41");
    }

    /// <summary>
    /// Spice Harvesting as played solo: the core rules (p. 21) with the solo criteria (p. 38) and the FAQ 3.1
    /// Ban change. Core rules that solo replaces (Stockpiling, saving spice by choice, a single Ban) are left out.
    /// </summary>
    public static ImmutableArray<string> SpiceHarvestingRules { get; } =
    [
        "Remove the surviving Harvesters: 1 spice for each in a Desert Area, 2 for each in a Deep Desert Area.",
        "Add the Spice Reserve. Solo: all spice is spent, the Reserve included.",
        $"Keeping a marker on its step costs {SpiceRules.KeepCost}; raising it 1 step costs {SpiceRules.RaiseCost} (at most 1 step per phase). Any other marker drops 1 step.",
        "Solo: first keep the markers from dropping, lowest first, then raise the lowest one(s).",
        $"Solo: with all markers on the top step and {SpiceRules.SupremacyThreshold}+ spice, the Harkonnens score 1 Supremacy point instead.",
        $"Spice that cannot be spent goes to the Spice Reserve (at most {SpiceRules.MaxReserve}); any more is lost.",
        "Every marker on the bottom step and every marker that just dropped activates its Ban (all of them, FAQ). Bans last until the next Spice Harvesting.",
    ];

    public static string ImperiumName(ImperiumPower power) => power switch
    {
        ImperiumPower.Choam => "CHOAM",
        ImperiumPower.SpacingGuild => "Spacing Guild",
        ImperiumPower.Landsraad => "Landsraad",
        _ => power.ToString(),
    };

    /// <summary>What happens to one Imperium marker, e.g. "raise (3)".</summary>
    public static string SpendLabel(MarkerOutcome marker) => marker.Change switch
    {
        MarkerChange.Raised => $"raise ({marker.Spent})",
        MarkerChange.Kept => $"keep ({marker.Spent})",
        MarkerChange.Dropped => "drops",
        MarkerChange.StayedAtBottom => "stays on the bottom step",
        _ => marker.Change.ToString(),
    };

    /// <summary>The Imperium markers whose Ban this spending activates or keeps active.</summary>
    public static ImmutableArray<ImperiumPower> BansAfter(SpiceResult result) =>
        [.. result.Markers.Where(m => m.Change is MarkerChange.Dropped or MarkerChange.StayedAtBottom).Select(m => m.Power)];

    /// <summary>One line summing up the spending, shown at the end of the round.</summary>
    public static string SpiceSummary(SpiceResult result)
    {
        var parts = result.Markers
            .Where(m => m.Change != MarkerChange.Kept)
            .Select(m => m.Change switch
            {
                MarkerChange.Raised => $"{ImperiumName(m.Power)} raised",
                MarkerChange.Dropped => $"{ImperiumName(m.Power)} dropped",
                _ => $"{ImperiumName(m.Power)} on the bottom step",
            })
            .ToList();

        if (parts.Count == 0)
        {
            parts.Add("all markers kept");
        }

        if (result.SupremacyGained)
        {
            parts.Add("Supremacy +1");
        }

        var bans = BansAfter(result);
        if (!bans.IsEmpty)
        {
            parts.Add($"{string.Join(", ", bans.Select(ImperiumName))} Ban active");
        }

        if (result.NewReserve > 0)
        {
            parts.Add($"Reserve {result.NewReserve}");
        }

        return $"Spice: {string.Join(" · ", parts)}";
    }

    public Guidance EndOfRound(GameState state)
    {
        var from = state.Supremacy;
        var to = Math.Min(content.Supremacy.Max, from + 1);
        var steps = ImmutableArray.CreateBuilder<GuidanceStep>();
        steps.Add(new("Atreides: claim End of the Round Prescience cards (at most 2 per round), then check your Secret Objective."));
        steps.Add(new("Remove all Ornithopters and Carryalls from the board."));
        steps.Add(new("You may replace Atreides Named Leaders on the board with Generic ones. Harkonnen Named Leaders stay (solo)."));
        steps.Add(new("Flip spent Leader cards faceup (the app marks them ready)."));
        steps.Add(new("Discard your Planning hand down to 6 cards. Never discard the Harkonnen Reinforcements (solo)."));
        steps.Add(new("Discard the claimed Prescience cards, and any unclaimed ones you choose."));
        steps.Add(new($"Advance the Harkonnen Supremacy marker 1 step on the board: from {from} to {to}.") { Important = true });

        foreach (var text in SupremacyEffects(state, to))
        {
            steps.Add(new(text));
        }

        steps.Add(new("The app reshuffles the 8 Tactical cards for the next round."));

        return new Guidance(
            $"End of Round {state.Round}",
            "Apply these steps on the board, then end the round.",
            null,
            steps.ToImmutable(),
            [
                "If all your Generic Leaders are on the board, you may still remove a Named Leader, without a replacement (FAQ).",
                "Leader cards of Leaders in the Regeneration Tank are flipped faceup but stay off the dashboard.",
                "Imperium Bans stay active until the next Spice Harvesting.",
            ],
            "Rulebook pp. 16 and 39, FAQ 3.1");
    }

    /// <summary>What reaching this Supremacy step brings: Leaders entering play, Bene Gesserit tokens, victory.</summary>
    private IEnumerable<string> SupremacyEffects(GameState state, int step)
    {
        foreach (var leader in content.Leaders.Where(l => l.Entry == LeaderEntry.SupremacyStep && l.EntryStep == step
                     && state.Leaders[l.Id].Status == LeaderStatus.NotInPlay))
        {
            yield return $"{leader.Name} enters play: place the Leader card on the {FaceName(leader.Face)} Action box.";
            if (leader.Replaces is { } replaced && state.Leaders[replaced].Status != LeaderStatus.Removed)
            {
                yield return $"{content.Leader(replaced).Name} is removed from the game (remove the figure if it is on the board).";
            }
        }

        if (content.Supremacy.AtreidesBeneGesseritSteps.Contains(step))
        {
            yield return "The Atreides take 1 Bene Gesserit token from the reserve.";
        }

        if (step >= content.Supremacy.Max)
        {
            yield return $"Supremacy {content.Supremacy.Max}: the Harkonnens win the game.";
        }
    }

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

        var others = LeaderRules.DeployChoices(content, state).Where(l => l != turn.DeployLeader).ToList();
        if (turn.DeployLeader is not null && others.Count > 0)
        {
            notes.Add($"If you can, you choose: {string.Join(", ", others.Select(l => content.Leader(l).Name))} could be deployed instead (tap the Leader to deploy).");
        }
        else if (turn.DeployLeader is { } first && content.Leader(first).DeployFirst)
        {
            notes.Add("Beast Rabban and Feyd-Rautha are deployed before any other Named Leader. A Bashar is deployed only when no Named Leader is available.");
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

    /// <summary>Order for applying Hits to a Harkonnen Legion (rulebook p. 41).</summary>
    public static ImmutableArray<GuidanceStep> HitPriority() =>
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

    private static string Plural(int count) => count == 1 ? string.Empty : "s";
}
