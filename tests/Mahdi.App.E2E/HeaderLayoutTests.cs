using Microsoft.Playwright;

namespace Mahdi.App.E2E;

/// <summary>
/// The header must fit on small phones too, in both orientations: nothing overlaps and nothing leaves the screen.
/// </summary>
[TestFixture("iPhone SE")]
[TestFixture("iPhone SE landscape")]
[TestFixture("Galaxy S8")]
[TestFixture("iPhone 15 Pro")]
[TestFixture("iPhone 15 Pro landscape")]
[Parallelizable(ParallelScope.Self)]
public sealed class HeaderLayoutTests(string device) : AppTest(device)
{
    private static readonly string[] HeaderItems = ["home-link", "phase", "supremacy", "undo", "theme-toggle"];

    /// <summary>The phase name may be shortened with an ellipsis, but must stay readable.</summary>
    private const float MinPhaseWidth = 60;

    [Test]
    public async Task HeaderItemsFitWithoutOverlapping()
    {
        await StartGameAsync(seed: 11);
        await AdvanceToActionsAsync(); // "Round 1 · Action Resolution" is one of the longest headers
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

            Assert.That(boxes.Single(b => b.Id == "phase").Box.Width, Is.GreaterThanOrEqualTo(MinPhaseWidth), "phase name is squeezed away");
        }
    }

    private static bool Overlaps(LocatorBoundingBoxResult a, LocatorBoundingBoxResult b) =>
        a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;
}
