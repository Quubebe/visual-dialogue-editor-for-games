namespace Dialogue.Core;

public class DialogueFlagState : IDialogueFlagSource
{
    private readonly Dictionary<string, bool> _values = new(StringComparer.Ordinal);
    public bool TryGetFlag(string flagName, out bool value)
    {
        return _values.TryGetValue(flagName, out value);
    }

    public void SetFlag(string flagName, bool value)
    {
        _values[flagName] = value;
    }

    public IReadOnlyDictionary<string, bool> GetSnapshot()
    {
        return new Dictionary<string, bool>(_values);
    }
}
