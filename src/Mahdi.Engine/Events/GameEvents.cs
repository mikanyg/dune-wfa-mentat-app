using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Mahdi.Engine.Model;

namespace Mahdi.Engine.Events;

/// <summary>
/// Facts appended to the game history. Random outcomes are recorded in the events,
/// so replaying the history always rebuilds the same state.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(SetupCompleted), "setupCompleted")]
[JsonDerivedType(typeof(DiceModeChanged), "diceModeChanged")]
[JsonDerivedType(typeof(RoundStarted), "roundStarted")]
[JsonDerivedType(typeof(RoundStartConfirmed), "roundStartConfirmed")]
[JsonDerivedType(typeof(VehiclesPlaced), "vehiclesPlaced")]
[JsonDerivedType(typeof(HarkonnenDieResolved), "dieResolved")]
[JsonDerivedType(typeof(HarkonnenTurnCompleted), "turnCompleted")]
[JsonDerivedType(typeof(TruthtranceChosen), "truthtrance")]
[JsonDerivedType(typeof(DesertHazardsConfirmed), "desertHazardsConfirmed")]
[JsonDerivedType(typeof(SpiceHarvested), "spiceHarvested")]
[JsonDerivedType(typeof(RoundEnded), "roundEnded")]
[JsonDerivedType(typeof(SietchDestroyed), "sietchDestroyed")]
[JsonDerivedType(typeof(TargetSietchDrawn), "targetDrawn")]
[JsonDerivedType(typeof(ReinforcementAdded), "reinforcementAdded")]
[JsonDerivedType(typeof(ReinforcementsDiscarded), "reinforcementsDiscarded")]
[JsonDerivedType(typeof(BeneGesseritGained), "beneGesseritGained")]
[JsonDerivedType(typeof(LeaderKilled), "leaderKilled")]
[JsonDerivedType(typeof(LeaderRemovedFromGame), "leaderRemoved")]
[JsonDerivedType(typeof(RageOvercameShaddamPlayed), "rageOvercameShaddam")]
[JsonDerivedType(typeof(NextDeckSet), "nextDeckSet")]
[JsonDerivedType(typeof(HawatsSchemingPlayed), "hawatsScheming")]
[JsonDerivedType(typeof(AtreidesVictoryDeclared), "atreidesVictory")]
public abstract record GameEvent;

public sealed record SetupCompleted : GameEvent;

public sealed record DiceModeChanged(DiceMode Mode) : GameEvent;

/// <param name="Rejected">Cards drawn for the Target Sietch and discarded (same Sector or destroyed Sietch).</param>
public sealed record RoundStarted(int Round, string HarvestCardId, string? TargetCardId, ImmutableArray<string> Rejected) : GameEvent;

public sealed record RoundStartConfirmed : GameEvent;

public sealed record VehiclesPlaced : GameEvent;

/// <param name="Rerolled">Results rolled first and blocked because 3 spent dice already show them.</param>
/// <param name="SpecialLeader">Named Leader whose special action replaces the regular Action.</param>
/// <param name="DeployLeader">Named Leader to deploy on a Deployment result (null deploys a Bashar).</param>
/// <param name="StartDeck">Deck to draw the first Planning card from on a Mentat result.</param>
public sealed record HarkonnenDieResolved(
    DieFace Face,
    ImmutableArray<DieFace> Rerolled,
    bool Forced,
    LeaderId? SpecialLeader,
    LeaderId? DeployLeader,
    PlanningDeck StartDeck) : GameEvent;

/// <param name="SpecialUsed">The Named Leader special action was taken (its card is spent).</param>
public sealed record HarkonnenTurnCompleted(bool SpecialUsed) : GameEvent;

public sealed record TruthtranceChosen(DieFace Face) : GameEvent;

public sealed record DesertHazardsConfirmed : GameEvent;

public sealed record SpiceHarvested(int DesertHarvesters, int DeepDesertHarvesters, SpiceResult Result) : GameEvent;

public sealed record RoundEnded : GameEvent;

public sealed record SietchDestroyed(string CardId, int Rank) : GameEvent;

/// <summary>A new Target Sietch was drawn mid-round (the previous one was destroyed).</summary>
/// <param name="Reshuffled">No eligible card was left, so the discarded cards were shuffled back first.</param>
public sealed record TargetSietchDrawn(string? TargetCardId, ImmutableArray<string> Rejected, bool Reshuffled) : GameEvent;

/// <summary>One Planning card added to the Reinforcements deck from the next deck in alternation.</summary>
public sealed record ReinforcementAdded(string Reason) : GameEvent;

public sealed record ReinforcementsDiscarded(int Count) : GameEvent;

public sealed record BeneGesseritGained : GameEvent;

public sealed record LeaderKilled(LeaderId Leader) : GameEvent;

public sealed record LeaderRemovedFromGame(LeaderId Leader) : GameEvent;

public sealed record RageOvercameShaddamPlayed : GameEvent;

public sealed record NextDeckSet(PlanningDeck Deck) : GameEvent;

public sealed record HawatsSchemingPlayed : GameEvent;

public sealed record AtreidesVictoryDeclared : GameEvent;
