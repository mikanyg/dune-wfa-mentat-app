using System.Collections.Immutable;
using Mahdi.Engine.Events;
using Mahdi.Engine.Model;

namespace Mahdi.Engine;

/// <summary>
/// Applies events to state. Pure and deterministic: all random or rule-derived outcomes
/// are already recorded in the events.
/// </summary>
public static class GameReducer
{
    /// <summary>Regeneration Tank "Start here" space; the Leader returns after the 1 space.</summary>
    public const int TankStart = 5;

    public static GameState Initial(GameContent content, DiceMode mode) => new()
    {
        DiceMode = mode,
        Imperium = Enum.GetValues<ImperiumPower>().ToImmutableDictionary(p => p, _ => content.TopStep),
        Tactical = TacticalState.Empty,
        Leaders = content.Leaders.ToImmutableDictionary(
            l => l.Id,
            l => l.Entry == LeaderEntry.AtStart ? LeaderState.InReserve : LeaderState.NotInPlay),
    };

    public static GameState Apply(GameContent content, GameState state, GameEvent e) => e switch
    {
        SetupCompleted => state,
        DiceModeChanged x => state with { DiceMode = x.Mode },
        RoundStarted x => StartRound(content, state, x),
        RoundStartConfirmed => state with { Phase = Phase.VehiclePlacement },
        VehiclesPlaced => state with { Phase = Phase.ActionResolution },
        HarkonnenDieResolved x => state with
        {
            Dice = state.Dice with
            {
                Pending = new PendingTurn(x.Face, x.Rerolled, x.Forced, x.SpecialLeader, x.DeployLeader, x.StartDeck),
                ForcedFace = x.Forced ? null : state.Dice.ForcedFace,
            },
        },
        HarkonnenTurnCompleted x => CompleteTurn(content, state, x),
        TruthtranceChosen x => state with { Dice = state.Dice with { ForcedFace = x.Face } },
        DesertHazardsConfirmed => state with { Phase = Phase.SpiceHarvesting },
        SpiceHarvested x => ApplySpice(content, state, x.Result),
        RoundEnded => EndRound(content, state),
        SietchDestroyed x => DestroySietch(content, state, x),
        TargetSietchDrawn x => DrawTarget(content, state, x),
        ReinforcementAdded => state with
        {
            Reinforcements = state.Reinforcements + 1,
            NextDeck = Other(state.NextDeck),
        },
        ReinforcementsDiscarded x => state with { Reinforcements = Math.Max(0, state.Reinforcements - x.Count) },
        BeneGesseritGained => GainBeneGesserit(content, state),
        LeaderKilled x => KillLeader(content, state, x.Leader),
        LeaderRemovedFromGame x => RemoveLeader(content, state, x.Leader),
        RageOvercameShaddamPlayed => EnterLeaders(content, state, LeaderEntry.WhenRageOvercameShaddamPlayed),
        NextDeckSet x => state with { NextDeck = x.Deck },
        HawatsSchemingPlayed => state.HawatsSchemingOnTable
            ? Notify(state with { HawatsSchemingOnTable = false }, NoticeKind.Important,
                "Second Hawat's Scheming: discard both cards and apply the card's usual effect.")
            : Notify(state with { HawatsSchemingOnTable = true }, NoticeKind.Info,
                "First Hawat's Scheming: place it next to the board. It has no effect until a second one is played."),
        AtreidesVictoryDeclared => state with { Outcome = GameOutcome.AtreidesVictory, Phase = Phase.GameOver },
        _ => throw new ArgumentOutOfRangeException(nameof(e), e.GetType().Name, "Unknown event."),
    };

    public static PlanningDeck Other(PlanningDeck deck) =>
        deck == PlanningDeck.Harkonnen ? PlanningDeck.Corrino : PlanningDeck.Harkonnen;

    private static GameState StartRound(GameContent content, GameState state, RoundStarted x)
    {
        var setAside = content.RowForStep(state.LowestImperiumStep).DiceSetAside;
        return state with
        {
            Phase = Phase.RoundStart,
            Round = x.Round,
            Tactical = state.Tactical with
            {
                Discard = x.Rejected,
                HarvestCardId = x.HarvestCardId,
                TargetCardId = x.TargetCardId,
            },
            Reinforcements = state.Reinforcements + 2,
            Dice = new DiceState
            {
                SetAside = setAside,
                Unused = content.Dice.DiceCount - setAside,
                ForcedFace = state.Dice.ForcedFace,
            },
        };
    }

    private static GameState CompleteTurn(GameContent content, GameState state, HarkonnenTurnCompleted x)
    {
        var turn = state.Dice.Pending ?? throw new InvalidOperationException("No Harkonnen turn in progress.");
        var leaders = state.Leaders;

        if (x.SpecialUsed && turn.SpecialLeader is { } special)
        {
            leaders = leaders.SetItem(special, leaders[special] with { CardSpent = true });
        }

        if (turn.Face == DieFace.Deployment && turn.DeployLeader is { } deployed && leaders[deployed].Status == LeaderStatus.InReserve)
        {
            leaders = leaders.SetItem(deployed, leaders[deployed] with { Status = LeaderStatus.OnBoard });
        }

        var nextDeck = state.NextDeck;
        if (turn.Face == DieFace.Mentat)
        {
            // Two cards drawn alternately leave the top discard from the other deck, so the next
            // draw starts from the same deck. A solo special draws 3 cards from a single deck.
            nextDeck = x.SpecialUsed && turn.SpecialLeader is { } mentat
                ? Other(content.Leader(mentat).Deck)
                : turn.StartDeck;
        }

        var dice = state.Dice with
        {
            Pending = null,
            Unused = state.Dice.Unused - 1,
            UsedFaces = state.Dice.UsedFaces.Add(turn.Face),
            TurnsTaken = state.Dice.TurnsTaken + 1,
        };

        var next = state with { Dice = dice, Leaders = leaders, NextDeck = nextDeck };
        next = AdvanceRegenerationTank(content, next);

        if (dice.Unused <= 0)
        {
            next = Notify(next with { Phase = Phase.DesertHazards }, NoticeKind.Important,
                "The Harkonnens spent their last die: Action Resolution is over. Any remaining Atreides actions are lost.");
        }

        return next;
    }

    private static GameState AdvanceRegenerationTank(GameContent content, GameState state)
    {
        var next = state;
        foreach (var (id, leader) in state.Leaders.Where(l => l.Value.Status == LeaderStatus.InTank))
        {
            var position = leader.TankPosition - 1;
            if (position <= 0)
            {
                next = Notify(next with
                {
                    Leaders = next.Leaders.SetItem(id, new LeaderState(LeaderStatus.InReserve, false, 0)),
                }, NoticeKind.Info, $"{content.Leader(id).Name} leaves the Regeneration Tank and is available again.");
            }
            else
            {
                next = next with { Leaders = next.Leaders.SetItem(id, leader with { TankPosition = position }) };
            }
        }

        return next;
    }

    private static GameState ApplySpice(GameContent content, GameState state, SpiceResult result)
    {
        var imperium = state.Imperium;
        foreach (var marker in result.Markers)
        {
            imperium = imperium.SetItem(marker.Power, marker.To);
        }

        // FAQ 3.1: every marker that moved down activates its Ban, and markers on the bottom step stay active.
        var bans = result.Markers
            .Where(m => m.Change == MarkerChange.Dropped || m.To == content.BottomStep)
            .Select(m => m.Power)
            .ToImmutableHashSet();

        var next = state with
        {
            Phase = Phase.EndOfRound,
            Imperium = imperium,
            ActiveBans = bans,
            SpiceReserve = result.NewReserve,
            LastSpiceResult = result,
        };

        foreach (var power in bans.Except(state.ActiveBans))
        {
            var ban = content.Ban(power);
            next = Notify(next, NoticeKind.Important, $"{ban.Name} Ban active: {ban.SoloEffect}");
        }

        return result.SupremacyGained ? AdvanceSupremacy(content, next, 1) : next;
    }

    private static GameState EndRound(GameContent content, GameState state)
    {
        var leaders = state.Leaders.ToImmutableDictionary(
            l => l.Key,
            l => l.Value.Status == LeaderStatus.InTank ? l.Value : l.Value with { CardSpent = false });

        var next = state with
        {
            Leaders = leaders,
            Tactical = TacticalState.Empty with { DestroyedSietches = state.Tactical.DestroyedSietches },
            Dice = new DiceState { ForcedFace = state.Dice.ForcedFace },
            LastSpiceResult = null,
        };

        return AdvanceSupremacy(content, next, 1);
    }

    private static GameState DestroySietch(GameContent content, GameState state, SietchDestroyed x)
    {
        var next = state with
        {
            Tactical = state.Tactical with { DestroyedSietches = state.Tactical.DestroyedSietches.Add(x.CardId) },
        };

        return AdvanceSupremacy(content, next, x.Rank);
    }

    private static GameState DrawTarget(GameContent content, GameState state, TargetSietchDrawn x)
    {
        var discard = x.Reshuffled ? [] : state.Tactical.Discard;
        if (state.Tactical.TargetCardId is { } old)
        {
            discard = discard.Add(old);
        }

        var next = state with
        {
            Tactical = state.Tactical with { Discard = discard.AddRange(x.Rejected), TargetCardId = x.TargetCardId },
        };

        return x.TargetCardId is { } id
            ? Notify(next, NoticeKind.Important, $"New Target Sietch: {content.Card(id).Sietch}.")
            : Notify(next, NoticeKind.Important, "No eligible Target Sietch remains this round.");
    }

    private static GameState GainBeneGesserit(GameContent content, GameState state)
    {
        if (state.Dice.SetAside > 0)
        {
            return Notify(state with
            {
                Dice = state.Dice with { SetAside = state.Dice.SetAside - 1, Unused = state.Dice.Unused + 1 },
            }, NoticeKind.Important, "Bene Gesserit token: take 1 Action die from The Spice Must Flow board and add it to the unused Harkonnen dice.");
        }

        return AdvanceSupremacy(
            content,
            Notify(state, NoticeKind.Important, "Bene Gesserit token: no die left on The Spice Must Flow board, so Supremacy +1 instead."),
            1);
    }

    private static GameState AdvanceSupremacy(GameContent content, GameState state, int amount)
    {
        var from = state.Supremacy;
        var to = Math.Min(content.Supremacy.Max, from + amount);
        var next = state with { Supremacy = to };

        for (var step = from + 1; step <= to; step++)
        {
            next = EnterLeaders(content, next, LeaderEntry.SupremacyStep, step);

            if (content.Supremacy.AtreidesBeneGesseritSteps.Contains(step))
            {
                next = Notify(next, NoticeKind.Important,
                    $"Supremacy reached step {step}: the Atreides take 1 Bene Gesserit token from the reserve.");
            }
        }

        if (to >= content.Supremacy.Max)
        {
            next = next with { Outcome = GameOutcome.HarkonnenVictory, Phase = Phase.GameOver };
        }

        return next;
    }

    private static GameState EnterLeaders(GameContent content, GameState state, LeaderEntry entry, int? step = null)
    {
        var next = state;
        foreach (var leader in content.Leaders.Where(l => l.Entry == entry && (step is null || l.EntryStep == step)))
        {
            if (next.Leaders[leader.Id].Status != LeaderStatus.NotInPlay)
            {
                continue;
            }

            next = Notify(next with { Leaders = next.Leaders.SetItem(leader.Id, LeaderState.InReserve) },
                NoticeKind.Important, $"{leader.Name} enters play: place the Leader card on the {leader.Face} Action box.");

            if (leader.Replaces is { } replaced && next.Leaders[replaced].Status != LeaderStatus.Removed)
            {
                next = Notify(next with
                {
                    Leaders = next.Leaders.SetItem(replaced, new LeaderState(LeaderStatus.Removed, true, 0)),
                }, NoticeKind.Important, $"{content.Leader(replaced).Name} is removed from the game (remove the figure if it is on the board).");
            }

            if (leader.GrantsBeneGesserit)
            {
                next = GainBeneGesserit(content, next);
            }
        }

        return next;
    }

    private static GameState KillLeader(GameContent content, GameState state, LeaderId id) =>
        Notify(state with
        {
            Leaders = state.Leaders.SetItem(id, new LeaderState(LeaderStatus.InTank, true, TankStart)),
        }, NoticeKind.Info, $"{content.Leader(id).Name} goes to the Regeneration Tank (Start space).");

    private static GameState RemoveLeader(GameContent content, GameState state, LeaderId id)
    {
        var next = Notify(state with
        {
            Leaders = state.Leaders.SetItem(id, new LeaderState(LeaderStatus.Removed, true, 0)),
        }, NoticeKind.Info, $"{content.Leader(id).Name} is removed from the game.");

        return id == LeaderId.ThufirHawat ? EnterLeaders(content, next, LeaderEntry.WhenHawatRemoved) : next;
    }

    private static GameState Notify(GameState state, NoticeKind kind, string text) =>
        state with { Notices = state.Notices.Add(new Notice(kind, text)) };
}
