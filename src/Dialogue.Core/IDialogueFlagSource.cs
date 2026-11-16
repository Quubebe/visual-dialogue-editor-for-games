namespace Dialogue.Core;

public interface IDialogueFlagSource
{
    bool TryGetFlag(string flagName, out bool value);
}
