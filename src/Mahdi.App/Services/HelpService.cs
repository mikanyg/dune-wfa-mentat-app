namespace Mahdi.App.Services;

public enum HelpTopic
{
    CombatPower,
    DesertHazards,
}

/// <summary>Opens and closes the rules help sheets. The sheet itself is hosted by the layout.</summary>
public sealed class HelpService
{
    public HelpTopic? Current { get; private set; }

    /// <summary>The part of the sheet to highlight, e.g. the attack a Desert Hazards link was opened from.</summary>
    public string? Focus { get; private set; }

    public event Action? Changed;

    public void Show(HelpTopic topic, string? focus = null)
    {
        Current = topic;
        Focus = focus;
        Changed?.Invoke();
    }

    public void Close()
    {
        Current = null;
        Focus = null;
        Changed?.Invoke();
    }
}
