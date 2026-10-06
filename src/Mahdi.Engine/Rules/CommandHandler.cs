using System.Collections.Immutable;
using Mahdi.Engine.Commands;
using Mahdi.Engine.Events;
using Mahdi.Engine.Model;

namespace Mahdi.Engine.Rules;

/// <summary>Validates a command against the current state and produces the resulting events.</summary>
public static class CommandHandler
{
    public static ImmutableArray<GameEvent> Handle(GameContent content, GameState state, GameCommand command, IRandomSource random)
    {
        var batch = new Batch(content, state);

        if (state.Phase == Phase.GameOver)
        {
            throw new CommandRejectedException("The game is over.");
        }

        switch (command)
        {
            case CompleteSetup:
                Require(state.Phase == Phase.Setup, "Setup is already complete.");
                batch.Emit(new SetupCompleted());
                batch.Emit(TacticalDeck.DrawRound(content, batch.State, random, 1));
                break;

            case ChangeDiceMode x:
                Require(x.Mode != state.DiceMode, "That dice mode is already selected.");
                batch.Emit(new DiceModeChanged(x.Mode));
                break;

            case ConfirmRoundStart:
                Require(state.Phase == Phase.RoundStart, "Not at the start of a round.");
                batch.Emit(new RoundStartConfirmed());
                break;

            case ConfirmVehicles:
                Require(state.Phase == Phase.VehiclePlacement, "Not in Vehicle Placement.");
                batch.Emit(new VehiclesPlaced());
                break;

            case RollHarkonnenDie:
                RequireTurnCanStart(state);
                Require(state.DiceMode == DiceMode.AppRolls || state.Dice.ForcedFace is not null,
                    "Physical dice mode: enter the result you rolled.");
                if (state.Dice.ForcedFace is { } forced)
                {
                    batch.Emit(Resolve(content, state, forced, [], true));
                }
                else
                {
                    var (face, rerolled) = DiceRules.Roll(content, state.Dice, random);
                    batch.Emit(Resolve(content, state, face, rerolled, false));
                }

                break;

            case EnterHarkonnenDie x:
                RequireTurnCanStart(state);
                if (state.Dice.ForcedFace is { } chosen)
                {
                    Require(x.Face == chosen, $"Truthtrance chose {chosen} for this turn.");
                    batch.Emit(Resolve(content, state, chosen, [], true));
                }
                else
                {
                    Require(!DiceRules.IsBlocked(content, state.Dice, x.Face),
                        $"3 spent dice already show {x.Face}: roll the die again.");
                    batch.Emit(Resolve(content, state, x.Face, [], false));
                }

                break;

            case CompleteHarkonnenTurn x:
                Require(state.Phase == Phase.ActionResolution && state.Dice.Pending is not null, "No Harkonnen turn in progress.");
                batch.Emit(new HarkonnenTurnCompleted(x.SpecialUsed && state.Dice.Pending!.SpecialLeader is not null));
                break;

            case PlayTruthtrance x:
                RequireTurnCanStart(state);
                Require(DiceRules.CanChooseWithTruthtrance(content, state.Dice, x.Face),
                    $"Truthtrance cannot choose {x.Face}: too many spent dice already show it.");
                batch.Emit(new TruthtranceChosen(x.Face));
                break;

            case ConfirmDesertHazards:
                Require(state.Phase == Phase.DesertHazards, "Not in Desert Hazards.");
                batch.Emit(new DesertHazardsConfirmed());
                break;

            case HarvestSpice x:
                Require(state.Phase == Phase.SpiceHarvesting, "Not in Spice Harvesting.");
                Require(x.DesertHarvesters >= 0 && x.DeepDesertHarvesters >= 0, "Harvester counts cannot be negative.");
                batch.Emit(new SpiceHarvested(
                    x.DesertHarvesters,
                    x.DeepDesertHarvesters,
                    SpiceRules.Resolve(content, state.Imperium, state.SpiceReserve,
                        SpiceRules.Collected(x.DesertHarvesters, x.DeepDesertHarvesters))));
                break;

            case EndRound:
                Require(state.Phase == Phase.EndOfRound, "Not at the end of the round.");
                batch.Emit(new RoundEnded());
                if (batch.State.Phase != Phase.GameOver)
                {
                    batch.Emit(TacticalDeck.DrawRound(content, batch.State, random, state.Round + 1));
                }

                break;

            case DestroySietch x:
                Require(content.TacticalCards.Any(c => c.Id == x.CardId), "Unknown Sietch.");
                Require(!state.Tactical.DestroyedSietches.Contains(x.CardId), "That Sietch is already destroyed.");
                Require(x.Rank is >= 1 and <= 3, "Sietch rank must be 1, 2 or 3.");
                batch.Emit(new SietchDestroyed(x.CardId, x.Rank));
                if (x.CardId == state.Tactical.TargetCardId && batch.State.Phase != Phase.GameOver)
                {
                    batch.Emit(TacticalDeck.RedrawTarget(content, batch.State, random));
                }

                break;

            case VoluntaryReveal:
                Require(!state.IsBanActive(ImperiumPower.SpacingGuild),
                    "The Spacing Guild Ban is active: voluntary reveals add no Reinforcements.");
                batch.Emit(new ReinforcementAdded("Voluntary reveal"));
                break;

            case DrawToReinforcements:
                batch.Emit(new ReinforcementAdded("Card effect"));
                break;

            case DiscardReinforcements x:
                Require(!state.IsBanActive(ImperiumPower.Landsraad),
                    "The Landsraad Ban is active: Reinforcements cannot be discarded for Combat dice.");
                Require(x.Count > 0 && x.Count <= state.Reinforcements, "Not enough Reinforcements cards.");
                batch.Emit(new ReinforcementsDiscarded(x.Count));
                break;

            case GainBeneGesserit:
                batch.Emit(new BeneGesseritGained());
                break;

            case KillLeader x:
                Require(state.Leaders[x.Leader].IsActive, "That Leader is not in play.");
                batch.Emit(new LeaderKilled(x.Leader));
                break;

            case RemoveLeaderFromGame x:
                Require(state.Leaders[x.Leader].Status is not (LeaderStatus.Removed or LeaderStatus.NotInPlay),
                    "That Leader is not in play.");
                batch.Emit(new LeaderRemovedFromGame(x.Leader));
                break;

            case PlayRageOvercameShaddam:
                Require(state.Leaders[LeaderId.ShaddamIV].Status == LeaderStatus.NotInPlay, "Shaddam IV is already in play.");
                batch.Emit(new RageOvercameShaddamPlayed());
                break;

            case SetNextDeck x:
                Require(x.Deck != state.NextDeck, "That deck is already next.");
                batch.Emit(new NextDeckSet(x.Deck));
                break;

            case PlayHawatsScheming:
                batch.Emit(new HawatsSchemingPlayed());
                break;

            case DeclareAtreidesVictory:
                batch.Emit(new AtreidesVictoryDeclared());
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(command), command.GetType().Name, "Unknown command.");
        }

        return batch.Events.ToImmutable();
    }

    private static HarkonnenDieResolved Resolve(GameContent content, GameState state, DieFace face, ImmutableArray<DieFace> rerolled, bool forced) =>
        new(
            face,
            rerolled,
            forced,
            LeaderRules.SpecialLeaderFor(content, state, face),
            face == DieFace.Deployment ? LeaderRules.DeployCandidates(content, state).Cast<LeaderId?>().FirstOrDefault() : null,
            state.NextDeck);

    private static void RequireTurnCanStart(GameState state)
    {
        Require(state.Phase == Phase.ActionResolution, "Not in Action Resolution.");
        Require(state.Dice.Pending is null, "Finish the current Harkonnen turn first.");
        Require(state.Dice.Unused > 0, "The Harkonnens have no unused dice.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new CommandRejectedException(message);
        }
    }

    /// <summary>Collects events and keeps a running state so later events can depend on earlier ones.</summary>
    private sealed class Batch(GameContent content, GameState state)
    {
        public GameState State { get; private set; } = state;

        public ImmutableArray<GameEvent>.Builder Events { get; } = ImmutableArray.CreateBuilder<GameEvent>();

        public void Emit(GameEvent e)
        {
            Events.Add(e);
            State = GameReducer.Apply(content, State, e);
        }
    }
}
