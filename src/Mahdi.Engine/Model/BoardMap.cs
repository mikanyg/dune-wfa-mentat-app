namespace Mahdi.Engine.Model;

/// <summary>A point on the schematic board map (see <see cref="BoardMap"/>).</summary>
public sealed record MapPoint(int X, int Y);

/// <summary>
/// The schematic board map drawn by the app, oriented like the Tactical cards: North at the top, the Atreides side
/// (South) at the bottom. The four Outer Sectors are the quadrants around the North Pole, minus the central circle,
/// which holds the Central Sectors (one Sector in the solo game).
/// </summary>
public static class BoardMap
{
    public const int Width = 120;
    public const int Height = 90;
    public const int CentreX = Width / 2;
    public const int CentreY = Height / 2;
    public const int CentralRadius = 27;

    /// <summary>The Sector containing a point of the map.</summary>
    public static Sector SectorAt(MapPoint point)
    {
        var dx = point.X - CentreX;
        var dy = point.Y - CentreY;
        if ((dx * dx) + (dy * dy) < CentralRadius * CentralRadius)
        {
            return Sector.Central;
        }

        return (dy < 0, dx < 0) switch
        {
            (true, true) => Sector.NorthWest,
            (true, false) => Sector.NorthEast,
            (false, true) => Sector.SouthWest,
            (false, false) => Sector.SouthEast,
        };
    }
}
