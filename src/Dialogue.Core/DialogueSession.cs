using System;

namespace Dialogue.Core;

public class DialogueSession
{
    private readonly DialogueDocument _document;
    public Guid? CurrentNodeId { get; private set; }
    public DialogueSession(DialogueDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _document = document;
        CurrentNodeId = document.StartNodeId;
    }

    public DialogueNode? GetCurrentNode()
    {
        if (CurrentNodeId is null)
        {
            return null;
        }
        return _document.Nodes.Find(node => node.Id == CurrentNodeId.Value);
    }

    public bool TryChoose(Guid choiceId)
    {
        DialogueNode? currentNode = GetCurrentNode();

        if (currentNode is null || currentNode.IsEnding) return false;

        DialogueChoice? choice = currentNode.Choices.Find(c => c.Id == choiceId);
        if (choice?.TargetNodeId is null) return false;

        Guid targetId = choice.TargetNodeId.Value;
        if (_document.Nodes.Find(n => n.Id == targetId) is null) return false;

        CurrentNodeId = targetId;
        return true;

    }
}
