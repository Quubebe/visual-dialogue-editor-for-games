namespace Dialogue.Core;

public class DialogueNode
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CharacterId { get; set; }
    public string Text { get; set; } = string.Empty;
    public List<DialogueChoice> Choices { get; set; } = new();
    public bool IsEnding { get; set; }
    public DialogueFlagCondition? ShowCondition { get; set; }
    public Guid? NextNodeId { get; set; }
}