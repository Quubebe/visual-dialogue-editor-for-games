using Dialogue.Core;

namespace Dialogue.Tests;

[TestClass]
public class DialogueValidatorTest
{
    [TestMethod]
    public void StartNode_MustExistInDocument()
    {
        var document = new DialogueDocument();

        var issues = DialogueValidator.Validate(document);
        Assert.HasCount(1, issues);
        Assert.IsTrue(issues[0].IsError);

        document.StartNodeId = Guid.NewGuid();
        issues = DialogueValidator.Validate(document);
        Assert.HasCount(1, issues);

        var start = new DialogueNode();
        document.Nodes.Add(start);
        document.StartNodeId = start.Id;

        issues = DialogueValidator.Validate(document);
        Assert.IsEmpty(issues);
    }

    [TestMethod]
    public void DuplicateNodeId_ReportsError()
    {
        var first = new DialogueNode();
        var second = new DialogueNode { Id = first.Id };

        var document = new DialogueDocument { StartNodeId = first.Id };
        document.Nodes.AddRange(new[] { first, second });

        var issues = DialogueValidator.Validate(document);

        Assert.HasCount(1, issues);
        Assert.IsTrue(issues[0].IsError);
        Assert.AreEqual<Guid?>(first.Id, issues[0].NodeId);
    }

    [TestMethod]
    public void ChoiceTarget_MustReferToExistingNode()
    {
        var start = new DialogueNode();
        var end = new DialogueNode { IsEnding = true };
        var choice = new DialogueChoice { Text = "Дальше" };
        start.Choices.Add(choice);

        start.Choices.Add(new DialogueChoice { Text = "Рабочий путь", TargetNodeId = end.Id });
        var document = new DialogueDocument { StartNodeId = start.Id };
        document.Nodes.AddRange(new[] { start, end });

        var issues = DialogueValidator.Validate(document);
        Assert.HasCount(1, issues);
        Assert.AreEqual<Guid?>(start.Id, issues[0].NodeId);

        choice.TargetNodeId = Guid.NewGuid();
        Assert.HasCount(1, DialogueValidator.Validate(document));

        choice.TargetNodeId = end.Id;
        Assert.IsEmpty(DialogueValidator.Validate(document));
    }

    [TestMethod]
    public void UnreachableNode_IsWarning_AndCycleIsAllowed()
    {
        var start = new DialogueNode { Text = "Начало." };
        var loop = new DialogueNode { Text = "Вернуться?" };
        var orphan = new DialogueNode { Text = "Черновик.", IsEnding = true };

        start.Choices.Add(new DialogueChoice { TargetNodeId = loop.Id });
        loop.Choices.Add(new DialogueChoice { TargetNodeId = start.Id });

        var document = new DialogueDocument { StartNodeId = start.Id };
        document.Nodes.AddRange(new[] { start, loop, orphan });

        var issues = DialogueValidator.Validate(document);

        Assert.HasCount(1, issues);
        Assert.IsFalse(issues[0].IsError);
        Assert.AreEqual<Guid?>(orphan.Id, issues[0].NodeId);
    }
}