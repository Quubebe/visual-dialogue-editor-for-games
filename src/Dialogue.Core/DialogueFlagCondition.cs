namespace Dialogue.Core;

public class DialogueFlagCondition
{
    public string FlagName { get; set; } = string.Empty;
    public bool ExpectedValue { get; set; }

    public bool Matches(IDialogueFlagSource flags)
    {
        ArgumentNullException.ThrowIfNull(flags);

        return flags.TryGetFlag(FlagName, out bool currentValue) && currentValue == ExpectedValue;
    }
}
