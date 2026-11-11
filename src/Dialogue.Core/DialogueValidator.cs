using System.Dynamic;

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

            if (node.CharacterId is Guid characterId && !document.Characters.Exists(character => character.Id == characterId))
            {
                issues.Add(new DialogueValidationIssue
                {
                    Message = "Реплика ссылается на отсутствующего персонажа.",
                    IsError = true,
                    NodeId = node.Id
                });
            }

            if (!node.IsEnding && node.Choices.Count == 0)
            {
                issues.Add(new DialogueValidationIssue
                {
                    Message = "У реплики нет ответов и она не отмечена как завершение.",
                    IsError = true,
                    NodeId = node.Id
                });
            }
        }

        var seenChoiceIds = new HashSet<Guid>();
        var seenCharacterIds = new HashSet<Guid>();

        foreach (DialogueCharacter character in document.Characters)
        {
            if (!seenCharacterIds.Add(character.Id))
            {
                issues.Add(new DialogueValidationIssue
                {
                    Message = $"Повторяется ID персонажа '{character.Name}': {character.Id}.",
                    IsError = true
                });
            }
        }

        foreach (DialogueNode node in document.Nodes)
        {
            foreach (DialogueChoice choice in node.Choices)
            {
                if (!seenChoiceIds.Add(choice.Id))
                {
                    issues.Add(new DialogueValidationIssue
                    {
                        Message = $"Повторяется ID ответа: {choice.Id}.",
                        IsError = true,
                        NodeId = node.Id
                    });
                }

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

        if (document.StartNodeId is Guid startId && document.Nodes.Exists(node => node.Id == startId))
        {
            HashSet<Guid> reachable = GetReachableNodeIds(document);
            foreach (DialogueNode node in document.Nodes)
            {
                if (!reachable.Contains(node.Id))
                {
                    issues.Add(new DialogueValidationIssue
                    {
                        Message = "Реплика недостижима от начала диалога.",
                        IsError = false,
                        NodeId = node.Id
                    });
                }
            }
        }

        return issues;
    }

    private static HashSet<Guid> GetReachableNodeIds(DialogueDocument document)
    {
        var reachable = new HashSet<Guid>();

        if (document.StartNodeId is not Guid startId) return reachable;

        var pending = new Queue<Guid>();
        pending.Enqueue(startId);

        while (pending.Count > 0)
        {
            Guid id = pending.Dequeue();
            DialogueNode? node = document.Nodes.Find(n => n.Id == id);

            if (node is null || !reachable.Add(id)) continue;
            if (node.IsEnding) continue;

            foreach (DialogueChoice choice in node.Choices)
            {
                if (choice.TargetNodeId is Guid targetId)
                {
                    pending.Enqueue(targetId);
                }
            }
        }
        return reachable;
    }
}
