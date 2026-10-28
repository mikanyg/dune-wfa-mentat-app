using Mahdi.Engine.Model;

namespace Mahdi.Engine.Commands;

/// <summary>Player intents. A command is validated and turned into one or more events.</summary>
public abstract record GameCommand;

public sealed record CompleteSetup : GameCommand;

public sealed record ChangeDiceMode(DiceMode Mode) : GameCommand;

public sealed record ConfirmRoundStart : GameCommand;

public sealed record ConfirmVehicles : GameCommand;

/// <summary>App-rolls mode: roll an unused Harkonnen die (rerolling blocked results).</summary>
public sealed record RollHarkonnenDie : GameCommand;

/// <summary>Physical mode: the face shown on the die the player rolled.</summary>
public sealed record EnterHarkonnenDie(DieFace Face) : GameCommand;

public sealed record CompleteHarkonnenTurn(bool SpecialUsed = true) : GameCommand;

public sealed record PlayTruthtrance(DieFace Face) : GameCommand;

public sealed record ConfirmDesertHazards : GameCommand;

public sealed record HarvestSpice(int DesertHarvesters, int DeepDesertHarvesters) : GameCommand;

public sealed record EndRound : GameCommand;

public sealed record DestroySietch(string CardId, int Rank) : GameCommand;

/// <summary>The Atreides voluntarily revealed this many Sietches or Deployment tokens at once.</summary>
public sealed record VoluntaryReveal(int Count = 1) : GameCommand;

/// <summary>A card effect lets the Harkonnens draw Planning cards into the Reinforcements deck.</summary>
public sealed record DrawToReinforcements(int Count = 1) : GameCommand;

/// <param name="LastDiscardedFrom">Deck of the last card discarded, which decides the next deck to draw from.</param>
public sealed record DiscardReinforcements(int Count, PlanningDeck? LastDiscardedFrom = null) : GameCommand;

public sealed record GainBeneGesserit : GameCommand;

public sealed record KillLeader(LeaderId Leader) : GameCommand;

public sealed record RemoveLeaderFromGame(LeaderId Leader) : GameCommand;

public sealed record PlayRageOvercameShaddam : GameCommand;

public sealed record SetNextDeck(PlanningDeck Deck) : GameCommand;

public sealed record PlayHawatsScheming : GameCommand;

public sealed record DeclareAtreidesVictory : GameCommand;

public sealed record StartBattle(
    Attacker Attacker,
    BattleTarget Target,
    string? SietchCardId = null,
    SettlementKind? Settlement = null) : GameCommand;

/// <summary>The current Combat round is over and the battle continues.</summary>
/// <param name="Discard">Reinforcements cards the Harkonnens discarded this round for Combat dice.</param>
/// <param name="LastDiscardedFrom">Deck of the last card discarded; required when <paramref name="Discard"/> is above 0.</param>
public sealed record FightBattleRound(int Discard = 0, PlanningDeck? LastDiscardedFrom = null) : GameCommand;

/// <summary>Ends the battle after its final round.</summary>
/// <param name="SietchRank">Rank of the Sietch the Harkonnens destroyed; required when a Sietch falls.</param>
/// <param name="KilledLeaders">Harkonnen Named Leaders killed in the battle (they go to the Regeneration Tank).</param>
public sealed record EndBattle(
    BattleOutcome Outcome,
    int Discard = 0,
    PlanningDeck? LastDiscardedFrom = null,
    int? SietchRank = null,
    IReadOnlyList<LeaderId>? KilledLeaders = null) : GameCommand;

/// <summary>Closes a battle opened by mistake, before any round was fought.</summary>
public sealed record CancelBattle : GameCommand;

public sealed class CommandRejectedException(string message) : InvalidOperationException(message);
