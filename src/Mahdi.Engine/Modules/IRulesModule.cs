namespace Mahdi.Engine.Modules;

/// <summary>
/// Seam for optional rules modules (expansions). Version 1 ships the base Mahdi Solo Mode only:
/// The Spacing Guild and Desert War are officially incompatible with solo play, and Smugglers
/// lacks solo rules for parts of its automa behaviour.
/// </summary>
public interface IRulesModule
{
    string Id { get; }

    string Name { get; }
}

public static class RulesModules
{
    public static IReadOnlyList<IRulesModule> Available { get; } = [];
}
