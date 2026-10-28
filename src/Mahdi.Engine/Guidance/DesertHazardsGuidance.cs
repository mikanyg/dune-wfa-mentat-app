using System.Collections.Immutable;
using Mahdi.Engine.Model;

namespace Mahdi.Engine.Guidance;

/// <summary>The Desert Hazards phase as three sections: Wormsigns, Sandworms and Coriolis Storms.</summary>
public sealed record DesertHazardsGuidance(ImmutableArray<HazardSection> Sections, ImmutableArray<string> Notes, string RuleReference);

/// <param name="Key">Stable name for the section (used for test ids).</param>
/// <param name="Lines">What to do, always shown.</param>
/// <param name="Details">Exceptions and edge cases, shown in an expandable section.</param>
/// <param name="Help">The attack whose Hits the help sheet explains, if the section links to it.</param>
public sealed record HazardSection(
    string Key,
    string Title,
    ImmutableArray<string> Lines,
    ImmutableArray<string> Details,
    HazardAttack? Help = null);
