using System.Collections.Immutable;
using Mahdi.Engine.Model;

namespace Mahdi.Engine.Rules;

public static class LeaderRules
{
    /// <summary>
    /// The Named Leader whose special action must be used for this result, if any. A Leader qualifies
    /// when it is in play, out of the Regeneration Tank and its card is not spent. Leaders whose special
    /// action moves their own Legion must also be on the board.
    /// </summary>
    public static LeaderId? SpecialLeaderFor(GameContent content, GameState state, DieFace face) =>
        content.Leaders
            .Where(l => l.Face == face)
            .Where(l => state.Leaders[l.Id] is { IsActive: true, CardSpent: false } leader
                && (!l.RequiresOnBoard || leader.Status == LeaderStatus.OnBoard))
            .Select(l => (LeaderId?)l.Id)
            .FirstOrDefault();

    /// <summary>
    /// Named Leaders that can be deployed (in play, figure off the board), in deployment priority:
    /// Beast Rabban and Feyd-Rautha come before any other Named Leader.
    /// </summary>
    public static ImmutableArray<LeaderId> DeployCandidates(GameContent content, GameState state) =>
    [
        .. content.Leaders
            .Where(l => state.Leaders[l.Id].Status == LeaderStatus.InReserve)
            .OrderByDescending(l => l.DeployFirst)
            .Select(l => l.Id),
    ];
}
