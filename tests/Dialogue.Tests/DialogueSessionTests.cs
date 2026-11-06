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
}
