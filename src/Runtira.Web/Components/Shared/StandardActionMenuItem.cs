namespace Runtira.Web.Components.Shared;

public sealed class StandardActionMenuItem
{
    public StandardActionMenuItem(string actionKey, string label, bool isDisabled = false, string? hint = null, bool isDestructive = false)
    {
        ActionKey = actionKey;
        Label = label;
        IsDisabled = isDisabled;
        Hint = hint;
        IsDestructive = isDestructive;
    }

    public string ActionKey { get; }
    public string Label { get; }
    public bool IsDisabled { get; }
    public string? Hint { get; }
    public bool IsDestructive { get; }
}
