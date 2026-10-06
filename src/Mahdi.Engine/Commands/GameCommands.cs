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

/// <summary>The Atreides voluntarily revealed a Sietch or Deployment token.</summary>
public sealed record VoluntaryReveal : GameCommand;

/// <summary>A card effect lets the Harkonnens draw a Planning card into the Reinforcements deck.</summary>
public sealed record DrawToReinforcements : GameCommand;

public sealed record DiscardReinforcements(int Count) : GameCommand;

public sealed record GainBeneGesserit : GameCommand;

public sealed record KillLeader(LeaderId Leader) : GameCommand;

public sealed record RemoveLeaderFromGame(LeaderId Leader) : GameCommand;

public sealed record PlayRageOvercameShaddam : GameCommand;

public sealed record SetNextDeck(PlanningDeck Deck) : GameCommand;

public sealed record PlayHawatsScheming : GameCommand;

public sealed record DeclareAtreidesVictory : GameCommand;

public sealed class CommandRejectedException(string message) : InvalidOperationException(message);
