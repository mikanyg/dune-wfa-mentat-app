using Mahdi.Engine.Guidance;
using Mahdi.Engine.Model;

namespace Mahdi.Engine.Tests;

public class PhaseLabelTests
{
    private static readonly string[] StepperBadges = ["S", "1", "2", "3", "4", "E"];
    private static readonly int[] PhaseNumbers = [1, 2, 3, 4];

    [TestCase(Phase.RoundStart, "Start of Round")]
    [TestCase(Phase.VehiclePlacement, "Phase 1: Vehicle Placement (Harkonnen)")]
    [TestCase(Phase.ActionResolution, "Phase 2: Action Resolution")]
    [TestCase(Phase.DesertHazards, "Phase 3: Desert Hazards (Atreides)")]
    [TestCase(Phase.SpiceHarvesting, "Phase 4: Spice Harvesting (Harkonnen)")]
    [TestCase(Phase.EndOfRound, "End of Round")]
    [TestCase(Phase.Setup, "Setup")]
    public void PhasesAreNamedAsInTheRulebook(Phase phase, string expected) =>
        Assert.That(PhaseLabel.For(phase).FullTitle, Is.EqualTo(expected));

    [TestCase(Phase.RoundStart, "Start Round", "S")]
    [TestCase(Phase.VehiclePlacement, "1: Vehicles", "1")]
    [TestCase(Phase.ActionResolution, "2: Actions", "2")]
    [TestCase(Phase.DesertHazards, "3: Hazards", "3")]
    [TestCase(Phase.SpiceHarvesting, "4: Harvest", "4")]
    [TestCase(Phase.EndOfRound, "End Round", "E")]
    public void NarrowScreensUseShortNamesAndBadges(Phase phase, string shortTitle, string badge)
    {
        var label = PhaseLabel.For(phase);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(label.ShortTitle, Is.EqualTo(shortTitle));
            Assert.That(label.Badge, Is.EqualTo(badge));
        }
    }

    [Test]
    public void TheStepperListsTheRoundInOrder() =>
        Assert.That(PhaseLabel.RoundPhases.Select(p => PhaseLabel.For(p).Badge), Is.EqualTo(StepperBadges).AsCollection);

    [Test]
    public void OnlyThePlayedPhasesAreNumbered()
    {
        var numbers = Enum.GetValues<Phase>().Select(p => PhaseLabel.For(p).Number).OfType<int>();
        Assert.That(numbers, Is.EqualTo(PhaseNumbers).AsCollection);
    }
}
