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

        var start = new DialogueNode { IsEnding = true };
        document.Nodes.Add(start);
        document.StartNodeId = start.Id;

        issues = DialogueValidator.Validate(document);
        Assert.IsEmpty(issues);
    }

    [TestMethod]
    public void DuplicateNodeId_ReportsError()
    {
        var first = new DialogueNode { IsEnding = true };
        var second = new DialogueNode { Id = first.Id, IsEnding = true };

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

    [TestMethod]
    public void DuplicateChoiceId_AcrossNodes_ReportsError()
    {
        var start = new DialogueNode();
        var second = new DialogueNode();

        var firstChoice = new DialogueChoice { TargetNodeId = second.Id };
        var secondChoice = new DialogueChoice
        {
            Id = firstChoice.Id,
            TargetNodeId = start.Id
        };

        start.Choices.Add(firstChoice);
        second.Choices.Add(secondChoice);

        var document = new DialogueDocument { StartNodeId = start.Id };
        document.Nodes.AddRange(new[] { start, second });

        var issues = DialogueValidator.Validate(document);

        Assert.HasCount(1, issues);
        Assert.IsTrue(issues[0].IsError);
        Assert.AreEqual<Guid?>(second.Id, issues[0].NodeId);
    }

    [TestMethod]
    public void DuplicateCharacterId_ReportsError()
    {
        var start = new DialogueNode { IsEnding = true };
        var first = new DialogueCharacter { Name = "Проводник" };
        var second = new DialogueCharacter { Id = first.Id, Name = "Страж" };

        var document = new DialogueDocument { StartNodeId = start.Id };
        document.Nodes.Add(start);
        document.Characters.AddRange(new[] { first, second });

        var issues = DialogueValidator.Validate(document);

        Assert.HasCount(1, issues);
        Assert.IsTrue(issues[0].IsError);
        Assert.IsNull(issues[0].NodeId);
    }

    [TestMethod]
    public void CharacterReference_MustExistOrBeEmpty()
    {
        Guid characterId = Guid.NewGuid();
        var start = new DialogueNode
        {
            IsEnding = true,
            CharacterId = characterId
        };

        var document = new DialogueDocument { StartNodeId = start.Id };
        document.Nodes.Add(start);

        var issues = DialogueValidator.Validate(document);
        Assert.HasCount(1, issues);
        Assert.AreEqual<Guid?>(start.Id, issues[0].NodeId);

        document.Characters.Add(new DialogueCharacter { Id = characterId });
        Assert.IsEmpty(DialogueValidator.Validate(document));

        start.CharacterId = null;
        Assert.IsEmpty(DialogueValidator.Validate(document));
    }
    
    [TestMethod]
    public void NodeWithoutChoices_MustBeMarkedAsEnding()
    {
        var node = new DialogueNode();
        var document = new DialogueDocument { StartNodeId = node.Id };
        document.Nodes.Add(node);

        var issues = DialogueValidator.Validate(document);

        Assert.HasCount(1, issues);
        Assert.IsTrue(issues[0].IsError);
        Assert.AreEqual<Guid?>(node.Id, issues[0].NodeId);

        node.IsEnding = true;
        Assert.IsEmpty(DialogueValidator.Validate(document));
    }
}