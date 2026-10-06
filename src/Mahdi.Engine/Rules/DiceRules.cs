using System.Collections.Immutable;
using Mahdi.Engine.Model;

namespace Mahdi.Engine.Rules;

public static class DiceRules
{
    /// <summary>A result is blocked when the dashboard already shows 3 spent dice with it: roll again.</summary>
    public static bool IsBlocked(GameContent content, DiceState dice, DieFace face) =>
        dice.UsedCount(face) >= content.Dice.SlotsPerResult;

    /// <summary>Truthtrance cannot pick a result with 3 spent dice (2 for Deployment and House).</summary>
    public static bool CanChooseWithTruthtrance(GameContent content, DiceState dice, DieFace face) =>
        dice.UsedCount(face) < content.Dice.TruthtranceLimits[face];

    /// <summary>Rolls one Harkonnen die, rerolling results blocked by the 3-spent-dice rule.</summary>
    public static (DieFace Face, ImmutableArray<DieFace> Rerolled) Roll(GameContent content, DiceState dice, IRandomSource random)
    {
        if (content.Dice.Faces.All(face => IsBlocked(content, dice, face)))
        {
            throw new InvalidOperationException("Every die result is blocked.");
        }

        var rerolled = ImmutableArray.CreateBuilder<DieFace>();
        while (true)
        {
            var face = content.Dice.Faces[random.Next(content.Dice.Faces.Length)];
            if (!IsBlocked(content, dice, face))
            {
                return (face, rerolled.ToImmutable());
            }

            rerolled.Add(face);
        }
    }
}
