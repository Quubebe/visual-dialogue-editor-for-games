using Dialogue.Core;

namespace Dialogue.Tests;

[TestClass]
public sealed class DialogueSessionTests
{
    [TestMethod]
    public void UnknownChoice_DoesNotPreventLaterValidChoice()
    {
        var start = new DialogueNode { Text = "Куда пойдём?" };
        var end = new DialogueNode { Text = "В лес.", IsEnding = true };
        var choice = new DialogueChoice { Text = "В лес", TargetNodeId = end.Id };
        start.Choices.Add(choice);

        var document = new DialogueDocument { StartNodeId = start.Id };
        document.Nodes.AddRange(new[] { start, end });

        var session = new DialogueSession(document);

        Assert.IsFalse(session.TryChoose(Guid.Empty));
        Assert.AreSame(start, session.GetCurrentNode());

        Assert.IsTrue(session.TryChoose(choice.Id));
        Assert.AreSame(end, session.GetCurrentNode());
    }

    [TestMethod]
    public void EndingNode_RejectsChoiceEvenIfOneIsPresent()
    {
        var ending = new DialogueNode { Text = "Конец.", IsEnding = true };
        var next = new DialogueNode { Text = "Лишняя карточка." };
        var choice = new DialogueChoice { Text = "Продолжить", TargetNodeId = next.Id };
        ending.Choices.Add(choice);

        var document = new DialogueDocument { StartNodeId = ending.Id };
        document.Nodes.AddRange(new[] { ending, next });

        var session = new DialogueSession(document);

        Assert.IsFalse(session.TryChoose(choice.Id));
        Assert.AreSame(ending, session.GetCurrentNode());
    }

    [TestMethod]
    public void Sessions_KeepSeparatePositions()
    {
        var start = new DialogueNode { Text = "Начало." };
        var end = new DialogueNode { Text = "Конец.", IsEnding = true };
        var choice = new DialogueChoice { Text = "Дальше", TargetNodeId = end.Id };
        start.Choices.Add(choice);

        var document = new DialogueDocument { StartNodeId = start.Id };
        document.Nodes.AddRange(new[] { start, end });

        var first = new DialogueSession(document);
        var second = new DialogueSession(document);

        Assert.IsTrue(first.TryChoose(choice.Id));
        Assert.AreSame(end, first.GetCurrentNode());
        Assert.AreSame(start, second.GetCurrentNode());
    }
    [TestMethod]
    public void ChangingQuestFlag_UpdatesVisibleChoicesAndNode()
    {
        var start = new DialogueNode { Text = "Начало" };
        var end = new DialogueNode
        {
            Text = "Спасибо за помощь",
            IsEnding = true,
            ShowCondition = new DialogueFlagCondition
            {
                FlagName = "quest",
                ExpectedValue = true
            }
        };

        var choice = new DialogueChoice
        {
            Text = "Продолжить",
            TargetNodeId = end.Id,
            ShowCondition = new DialogueFlagCondition
            {
                FlagName = "quest",
                ExpectedValue = true
            }
        };
        start.Choices.Add(choice);

        var document = new DialogueDocument { StartNodeId = start.Id };
        document.Nodes.AddRange(new[] { start, end });

        var flags = new DialogueFlagState();
        var session = new DialogueSession(document, flags);

        Assert.IsEmpty(session.GetAvailableChoices());
        Assert.IsFalse(session.TryChoose(choice.Id));

        flags.SetFlag("quest", false);
        Assert.IsEmpty(session.GetAvailableChoices());

        flags.SetFlag("quest", true);
        Assert.Contains(choice, session.GetAvailableChoices());
        Assert.IsTrue(session.TryChoose(choice.Id));
        Assert.AreSame(end, session.GetCurrentNode());
    }

    [TestMethod]
    public void ChoiceReturnsQuestChange_AndCallerAppliesIt()
    {
        var start = new DialogueNode { Text = "Поможешь?" };
        var end = new DialogueNode
        {
            Text = "Спасибо за помощь",
            IsEnding = true,
            ShowCondition = new DialogueFlagCondition
            {
                FlagName = "quest",
                ExpectedValue = true
            }
        };

        var choice = new DialogueChoice
        {
            Text = "Обещаю помочь",
            TargetNodeId = end.Id
        };
        choice.FlagChanges.Add(new DialogueFlagChange
        {
            FlagName = "quest",
            NewValue = true
        });
        start.Choices.Add(choice);

        var document = new DialogueDocument { StartNodeId = start.Id };
        document.Nodes.AddRange(new[] { start, end });

        var flags = new DialogueFlagState();
        flags.SetFlag("quest", false);
        var session = new DialogueSession(document, flags);

        Assert.IsFalse(session.TryChoose(choice.Id));
        Assert.AreSame(start, session.GetCurrentNode());

        Assert.IsTrue(session.TryChoose(choice.Id, out var changes));
        Assert.HasCount(1, changes);
        Assert.AreEqual("quest", changes[0].FlagName);
        Assert.IsTrue(changes[0].NewValue);

        Assert.IsNull(session.GetCurrentNode());

        foreach (var change in changes)
            flags.SetFlag(change.FlagName, change.NewValue);

        Assert.AreSame(end, session.GetCurrentNode());
    }

    [TestMethod]
    public void Continue_MovesToNextLineWithoutPlayerChoice()
    {
        var second = new DialogueNode
        {
            Text = "И ещё кое-что...",
            IsEnding = true
        };
        var first = new DialogueNode
        {
            Text = "Здравствуй.",
            NextNodeId = second.Id
        };

        var document = new DialogueDocument { StartNodeId = first.Id };
        document.Nodes.AddRange(new[] { first, second });

        var session = new DialogueSession(document);

        Assert.AreSame(first, session.GetCurrentNode());
        Assert.IsEmpty(session.GetAvailableChoices());
        Assert.IsTrue(session.TryContinue());
        Assert.AreSame(second, session.GetCurrentNode());
        Assert.IsFalse(session.TryContinue());
    }
}
