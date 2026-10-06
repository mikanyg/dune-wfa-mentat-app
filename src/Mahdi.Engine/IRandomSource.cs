namespace Mahdi.Engine;

public interface IRandomSource
{
    /// <summary>Returns a value in [0, maxExclusive).</summary>
    int Next(int maxExclusive);
}

/// <summary>
/// Deterministic randomness: each command gets its own stream derived from the game seed and
/// the command's position in the history, so undoing and redoing a roll gives the same result.
/// </summary>
public sealed class SeededRandomSource(int seed, int commandIndex) : IRandomSource
{
    private readonly Random random = new(unchecked((seed * 486187739) + (commandIndex * 16777619)));

    public int Next(int maxExclusive) => random.Next(maxExclusive);
}
