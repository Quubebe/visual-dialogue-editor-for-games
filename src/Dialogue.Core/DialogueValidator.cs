using System.ComponentModel.DataAnnotations.Schema;
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

            if (!node.IsEnding && node.Choices.Count == 0 && node.NextNodeId is null)
            {
                issues.Add(new DialogueValidationIssue
                {
                    Message = "У реплики нет ответов, следующей реплики или завершения.",
                    IsError = true,
                    NodeId = node.Id
                });
            }

            if (node.NextNodeId is Guid nextId && !document.Nodes.Exists(target => target.Id == nextId))
            {
                issues.Add(new DialogueValidationIssue
                {
                    Message = "Следующая реплика не существует.",
                    IsError = true,
                    NodeId = node.Id
                });
            }

            if (node.IsEnding && (node.NextNodeId is not null || node.Choices.Count > 0))
            {
                issues.Add(new DialogueValidationIssue
                {
                    Message = "Завершающая реплика не должна иметь ответов или следующей реплики.",
                    IsError = true,
                    NodeId = node.Id
                });
            }
            else if (node.NextNodeId is not null && node.Choices.Count > 0)
            {
                issues.Add(new DialogueValidationIssue
                {
                    Message = "Выберите один способ продолжения: ответы или следующая реплика.",
                    IsError = true,
                    NodeId = node.Id
                });
            }

            if (node.ShowCondition is { } condition && string.IsNullOrWhiteSpace(condition.FlagName))
            {
                issues.Add(new DialogueValidationIssue
                {
                    Message = "условие показа реплики: не указано имя флага.",
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

                if (choice.ShowCondition is { } condition && string.IsNullOrWhiteSpace(condition.FlagName))
                {
                    issues.Add(new DialogueValidationIssue
                    {
                        Message = $"Условие показа ответа '{choice.Text}': не указано имя флага.",
                        IsError = true,
                        NodeId = node.Id
                    });
                }

                foreach (DialogueFlagChange change in choice.FlagChanges)
                {
                    if (string.IsNullOrWhiteSpace(change.FlagName))
                    {

                        issues.Add(new DialogueValidationIssue
                        {
                            Message = $"Пследствие ответа '{choice.Text}': не указано имя флага.",
                            IsError = true,
                            NodeId = node.Id
                        });
                    }
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

        foreach (DialogueValidationIssue issue in issues)
        {
            if (issue.NodeId is not Guid nodeId)
                continue;

            DialogueNode? node = document.Nodes.Find(n => n.Id == nodeId);
            if (node is null)
                continue;

            string title = string.IsNullOrWhiteSpace(node.Text)
                ? "без текста"
                : node.Text.Replace('\r', ' ').Replace('\n', ' ').Trim();

            if (title.Length > 32)
                title = title[..32] + "…";

            issue.Message =
                $"«{title}» [{nodeId.ToString()[..8]}]\n{issue.Message}";
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
            if (node.NextNodeId is Guid nextId) pending.Enqueue(nextId);

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
