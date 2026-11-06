namespace Dialogue.Core;

public class DialogueDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int FormatVersion { get; set; } = 1;
    public string Title { get; set; } = string.Empty;
    public List<DialogueCharacter> Characters { get; set; } = new();
    public List<DialogueNode> Nodes { get; set; } = new();
    public Guid? StartNodeId { get; set; }
}
