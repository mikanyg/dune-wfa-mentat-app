using System.Collections.Immutable;

namespace Mahdi.Engine.Guidance;

/// <summary>An instruction card: a headline plus an ordered list (a fallback chain or a priority list).</summary>
/// <param name="StepsLabel">How to read the steps, e.g. "Do the first one that is possible".</param>
/// <param name="Alternative">Regular Action to use when a Named Leader special action is not possible.</param>
/// <param name="RuleReference">Rulebook page reference for the "why" disclosure.</param>
public sealed record Guidance(
    string Title,
    string Headline,
    string? StepsLabel,
    ImmutableArray<GuidanceStep> Steps,
    ImmutableArray<string> Notes,
    string? RuleReference = null,
    Guidance? Alternative = null);

/// <param name="Details">Tie-breakers or sub-steps, shown in an expandable section.</param>
public sealed record GuidanceStep(string Text, ImmutableArray<string> Details)
{
    public GuidanceStep(string text)
        : this(text, [])
    {
    }
}
