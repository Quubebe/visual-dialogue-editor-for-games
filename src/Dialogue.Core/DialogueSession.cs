using System;

namespace Dialogue.Core;

public class DialogueSession
{
    private readonly IDialogueFlagSource? _flags;
    private readonly DialogueDocument _document;
    public Guid? CurrentNodeId { get; private set; }

    public DialogueNode? GetCurrentNode()
    {
        if (CurrentNodeId is null)
        {
            return null;
        }

        DialogueNode? node = _document.Nodes.Find(node => node.Id == CurrentNodeId.Value);
        return node is not null && IsVisible(node.ShowCondition) ? node : null;
    }

    public bool TryChoose(Guid choiceId)
    {
        return TryChooseCore(choiceId, allowFlagChanges: false, out _);
    }

    public bool TryChoose(Guid choiceId, out IReadOnlyList<DialogueFlagChange> changes)
    {
        return TryChooseCore(choiceId, allowFlagChanges: true, out changes);
    }

    private bool TryChooseCore(Guid choiceId, bool allowFlagChanges, out IReadOnlyList<DialogueFlagChange> changes)
    {
        changes = Array.Empty<DialogueFlagChange>();

        DialogueNode? currentNode = GetCurrentNode();

        if (currentNode is null || currentNode.IsEnding || currentNode.NextNodeId is not null) return false;

        DialogueChoice? choice = currentNode.Choices.Find(c => c.Id == choiceId);
        if (choice?.TargetNodeId is null || !IsVisible(choice.ShowCondition)) return false;

        Guid targetId = choice.TargetNodeId.Value;
        if (_document.Nodes.Find(n => n.Id == targetId) is null) return false;

        if (!allowFlagChanges && choice.FlagChanges.Count > 0) return false;

        changes = choice.FlagChanges.ToArray();
        CurrentNodeId = targetId;
        return true;
    }

    public DialogueSession(DialogueDocument document, IDialogueFlagSource? flags = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        _document = document;
        _flags = flags;
        CurrentNodeId = document.StartNodeId;
    }

    private bool IsVisible(DialogueFlagCondition? condition)
    {
        return condition is null || (_flags is not null && condition.Matches(_flags));
    }

    public List<DialogueChoice> GetAvailableChoices()
    {
        DialogueNode? node = GetCurrentNode();
        if (node is null || node.IsEnding || node.NextNodeId is not null) return new();

        return node.Choices.FindAll(choice => IsVisible(choice.ShowCondition));
    }

    public bool TryContinue()
    {
        DialogueNode? current = GetCurrentNode();

        if (current is null ||
            current.IsEnding ||
            current.Choices.Count > 0 ||
            current.NextNodeId is not Guid targetId)
            return false;

        DialogueNode? target = _document.Nodes.Find(
            node => node.Id == targetId);

        if (target is null || !IsVisible(target.ShowCondition))
            return false;

        CurrentNodeId = targetId;
        return true;
    }
}
