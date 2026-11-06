using System;

namespace Dialogue.Core;

public static class DialogueValidator
{
    public static List<DialogueValidationIssue> Validate(DialogueDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var issues = new List<DialogueValidationIssue>();
        if (document.StartNodeId is null || !document.Nodes.Exists(node => node.Id == document.StartNodeId))
        {
            issues.Add(new DialogueValidationIssue
            {
                Message = "Не выбрана существующая начальная реплика.",
                IsError = true
            });
        }

        var seenNodeIds = new HashSet<Guid>();

        foreach (DialogueNode node in document.Nodes)
        {
            if (!seenNodeIds.Add(node.Id))
            {
                issues.Add(new DialogueValidationIssue
                {
                    Message = $"Повторяется ID реплики:{node.Id}.",
                    IsError = true,
                    NodeId = node.Id
                });
            }
        }

        foreach (DialogueNode node in document.Nodes)
        {
            foreach (DialogueChoice choice in node.Choices)
            {
                if (choice.TargetNodeId is null || !document.Nodes.Exists(target => target.Id == choice.TargetNodeId))
                {
                    issues.Add(new DialogueValidationIssue
                    {
                        Message = $"Ответ «{choice.Text}» не ведет к существующей реплике.",
                        IsError = true,
                        NodeId = node.Id
                    });
                }
            }
        }

        return issues;
    }
}
