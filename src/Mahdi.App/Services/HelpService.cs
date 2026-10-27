namespace Mahdi.App.Services;

public enum HelpTopic
{
    CombatPower,
}

/// <summary>Opens and closes the rules help sheets. The sheet itself is hosted by the layout.</summary>
public sealed class HelpService
{
    public HelpTopic? Current { get; private set; }

    public event Action? Changed;

    public void Show(HelpTopic topic)
    {
        Current = topic;
        Changed?.Invoke();
    }

    public void Close()
    {
        Current = null;
        Changed?.Invoke();
    }
}
