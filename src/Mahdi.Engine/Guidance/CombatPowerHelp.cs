using System.Collections.Immutable;

namespace Mahdi.Engine.Guidance;

/// <summary>The Combat Power rules (rulebook p. 39) as shown in the in-app help sheet.</summary>
/// <param name="Counting">How a Legion's Combat Power is counted.</param>
/// <param name="Example">A worked example; its points add up to <see cref="ExampleTotal"/>.</param>
/// <param name="TieValues">Values of single figures, used on a tie or when single Units are compared.</param>
public sealed record CombatPowerHelp(
    ImmutableArray<CombatPowerRow> Counting,
    ImmutableArray<CombatPowerRow> Example,
    ImmutableArray<CombatPowerRow> TieValues,
    ImmutableArray<string> Notes,
    string RuleReference)
{
    public int ExampleTotal => Example.Sum(row => row.Points);

    public static CombatPowerHelp Rules { get; } = new(
        [
            new("Each Unit (any type)", 1),
            new("Each Deployment token", 2),
            new("Each Leader (Generic or Named)", 1),
        ],
        [
            new("3 Regular Units", 3),
            new("1 Elite Unit", 1),
            new("1 Deployment token", 2),
            new("1 Bashar Leader", 1),
        ],
        [
            new("Generic Leader (Bashar, Naib)", 1),
            new("Regular Unit", 2),
            new("Named Leader", 2),
            new("Elite Unit", 3),
            new("Sardaukar or Fedaykin Unit", 4),
        ],
        [
            "An attacking or moving Harkonnen Legion must have a greater Combat Power than the Atreides Legion it compares with; equal is not enough.",
            "Sietch and Settlement rank do not count towards Combat Power.",
            "In battle the Harkonnens stop attacking at the start of a Combat round if their Combat Power is half or less of the opposing Legion's (count single Units with the tie values).",
        ],
        "Rulebook pp. 39 and 41");
}

public sealed record CombatPowerRow(string Label, int Points);
