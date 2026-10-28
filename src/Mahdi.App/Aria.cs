namespace Mahdi.App;

public static class Aria
{
    /// <summary>
    /// ARIA state attributes need the literal strings "true" or "false"; Blazor renders a C# true as an
    /// empty attribute value, which assistive technology does not read as pressed or checked.
    /// </summary>
    public static string AriaBool(bool value) => value ? "true" : "false";
}
