using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Mahdi.App.E2E;

/// <summary>
/// The header must fit on small phones too, in both orientations, and on a wide desktop window (where the phase label
/// moves to the centre): nothing overlaps and nothing leaves the screen.
/// </summary>
[TestFixture("iPhone SE")]
[TestFixture("iPhone SE landscape")]
[TestFixture("Galaxy S8")]
[TestFixture("iPhone 15 Pro")]
[TestFixture("iPhone 15 Pro landscape")]
[TestFixture("iPad Pro 11 landscape")]
[TestFixture("Desktop 1500x900")]
[TestFixture("Desktop 1920x1080")]
[Parallelizable(ParallelScope.Self)]
public sealed partial class HeaderLayoutTests(string device) : AppTest(device)
{
    private static readonly string[] HeaderItems = ["home-link", "round", "phase", "supremacy", "undo", "theme-toggle"];

    /// <summary>The phase label must stay readable: at least the stepper badge.</summary>
    private const float MinPhaseWidth = 16;

    /// <summary>From this width the phase label sits in the centre of the header (game.css).</summary>
    private const int CentredFrom = 1000;

    /// <summary>From this width the full phase name is shown; below, the short name.</summary>
    private const int FullNameFrom = 1500;

    /// <summary>Up to this width the phase shows as its stepper badge.</summary>
    private const int BadgeUpTo = 520;

    [GeneratedRegex(@"^\s*Phase 1:\s+Vehicle Placement\s+\(Harkonnen\)\s*$")]
    private static partial Regex FullVehicleLabel();

    [Test]
    public async Task HeaderItemsFitWithoutOverlapping()
    {
        await StartGameAsync(seed: 11);
        await Tap("confirm-phase");
        await Tap("confirm-phase"); // Vehicle Placement: the longest label, "Phase 1: Vehicle Placement (Harkonnen)"
        await ExpectPhaseAsync("VehiclePlacement");
        await SnapshotAsync("header");

        var viewport = Page.ViewportSize!;
        var boxes = new List<(string Id, LocatorBoundingBoxResult Box)>();
        foreach (var id in HeaderItems)
        {
            var box = await ById(id).BoundingBoxAsync();
            Assert.That(box, Is.Not.Null, $"{id} is not visible");
            boxes.Add((id, box!));
        }

        using (Assert.EnterMultipleScope())
        {
            foreach (var (id, box) in boxes)
            {
                Assert.That(box.X, Is.GreaterThanOrEqualTo(0), $"{id} starts off the screen");
                Assert.That(box.X + box.Width, Is.LessThanOrEqualTo(viewport.Width), $"{id} ends off the screen");
            }

            for (var i = 0; i < boxes.Count; i++)
            {
                for (var j = i + 1; j < boxes.Count; j++)
                {
                    Assert.That(Overlaps(boxes[i].Box, boxes[j].Box), Is.False, $"{boxes[i].Id} overlaps {boxes[j].Id}");
                }
            }

            var phase = boxes.Single(b => b.Id == "phase").Box;
            Assert.That(phase.Width, Is.GreaterThanOrEqualTo(MinPhaseWidth), "phase name is squeezed away");
            if (viewport.Width >= CentredFrom)
            {
                Assert.That(phase.X + (phase.Width / 2), Is.EqualTo(viewport.Width / 2f).Within(2), "the phase label is centred");
            }
        }

        var visibleText = new LocatorAssertionsToHaveTextOptions { UseInnerText = true };
        if (viewport.Width >= FullNameFrom)
        {
            await Expect(ById("phase")).ToHaveTextAsync(FullVehicleLabel(), visibleText);
        }
        else if (viewport.Width > BadgeUpTo)
        {
            await Expect(ById("phase")).ToHaveTextAsync("1: Vehicles", visibleText);
        }
        else
        {
            await Expect(ById("phase")).ToHaveTextAsync("1", visibleText);
        }
    }

    private static bool Overlaps(LocatorBoundingBoxResult a, LocatorBoundingBoxResult b) =>
        a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;
}
