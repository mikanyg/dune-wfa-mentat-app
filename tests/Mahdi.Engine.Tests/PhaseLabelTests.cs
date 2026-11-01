using Mahdi.Engine.Guidance;
using Mahdi.Engine.Model;

namespace Mahdi.Engine.Tests;

public class PhaseLabelTests
{
    [TestCase(Phase.RoundStart, "Start of Round")]
    [TestCase(Phase.VehiclePlacement, "Phase 1: Vehicle Placement (Harkonnen)")]
    [TestCase(Phase.ActionResolution, "Phase 2: Action Resolution")]
    [TestCase(Phase.DesertHazards, "Phase 3: Desert Hazards (Atreides)")]
    [TestCase(Phase.SpiceHarvesting, "Phase 4: Spice Harvesting (Harkonnen)")]
    [TestCase(Phase.EndOfRound, "End of Round")]
    [TestCase(Phase.Setup, "Setup")]
    public void PhasesAreNamedAsInTheRulebook(Phase phase, string expected) =>
        Assert.That(PhaseLabel.For(phase).FullTitle, Is.EqualTo(expected));

    [Test]
    public void OnlyThePlayedPhasesAreNumbered()
    {
        var numbers = Enum.GetValues<Phase>().Select(p => PhaseLabel.For(p).Number).OfType<int>();
        Assert.That(numbers, Is.EqualTo(new[] { 1, 2, 3, 4 }).AsCollection);
    }
}
