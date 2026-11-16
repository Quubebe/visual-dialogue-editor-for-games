using System;

namespace Dialogue.Core;

public class DialogueChoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Text { get; set; } = string.Empty;
    public Guid? TargetNodeId { get; set; }
    public DialogueFlagCondition? ShowCondition { get; set; }
    public List<DialogueFlagChange> FlagChanges { get; set; } = new();
}
