using Dialogue.Core;
using Dialogue.Persistence;

namespace Dialogue.Tests;

[TestClass]
public sealed class FlowerQuestScenarioTests
{
    private static DialogueProject LoadExample() =>
        DialogueProjectFileStorage.Load(
            Path.Combine(AppContext.BaseDirectory, "FlowerQuest.dproj"));

    [TestMethod]
    public void Example_SaveReopenAndExport_PreservesDialogueAndLayout()
    {
        DialogueProject source = LoadExample();
        List<DialogueValidationIssue> errors = DialogueValidator.Validate(source.Dialogue)
            .Where(issue => issue.IsError).ToList();
        Assert.IsEmpty(errors, string.Join(Environment.NewLine, errors.Select(issue => issue.Message)));

        string directory = Path.Combine(Path.GetTempPath(), "DialogueScenario-" + Guid.NewGuid());
        Directory.CreateDirectory(directory);

        try
        {
            string projectPath = Path.Combine(directory, "flower.dproj");
            string exportPath = Path.Combine(directory, "flower.json");

            DialogueProjectFileStorage.Save(projectPath, source);
            DialogueProject reopened = DialogueProjectFileStorage.Load(projectPath);

            Assert.HasCount(2, reopened.Dialogue.Characters);
            Assert.HasCount(6, reopened.Dialogue.Nodes);
            Assert.HasCount(6, reopened.NodePositions);
            Assert.AreEqual(930.0, reopened.NodePositions[3].X);

            DialogueFileStorage.Save(exportPath, reopened.Dialogue);
            DialogueDocument exported = DialogueFileStorage.Load(exportPath);

            Assert.AreEqual(reopened.Dialogue.StartNodeId, exported.StartNodeId);
            Assert.HasCount(6, exported.Nodes);
            Assert.IsFalse(File.ReadAllText(exportPath).Contains("NodePositions", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public void Example_WithFlower_AppliesQuestAndFlowerChanges()
    {
        DialogueProject project = LoadExample();
        var flags = new DialogueFlagState();
        flags.SetFlag("quest", false);
        flags.SetFlag("has_flower", true);
        var session = new DialogueSession(project.Dialogue, flags);

        DialogueChoice promise = session.GetAvailableChoices()
            .Single(choice => choice.Text == "Обещаю помочь");
        Assert.IsTrue(session.TryChoose(promise.Id, out var questChanges));
        foreach (DialogueFlagChange change in questChanges)
            flags.SetFlag(change.FlagName, change.NewValue);

        Assert.AreEqual("Спасибо. Загляни в сад.", session.GetCurrentNode()?.Text);
        Assert.IsTrue(session.TryContinue());
        Assert.AreEqual("Ты нашла цветок?", session.GetCurrentNode()?.Text);

        DialogueChoice giveFlower = session.GetAvailableChoices()
            .Single(choice => choice.Text == "Отдать цветок");
        Assert.IsTrue(session.TryChoose(giveFlower.Id, out var flowerChanges));
        Assert.HasCount(2, flowerChanges);
        foreach (DialogueFlagChange change in flowerChanges)
            flags.SetFlag(change.FlagName, change.NewValue);

        Assert.AreEqual("Спасибо за цветок!", session.GetCurrentNode()?.Text);
        Assert.IsTrue(flags.TryGetFlag("quest", out bool quest) && quest);
        Assert.IsTrue(flags.TryGetFlag("has_flower", out bool hasFlower) && !hasFlower);
        Assert.IsTrue(flags.TryGetFlag("flower_given", out bool flowerGiven) && flowerGiven);
    }

    [TestMethod]
    public void Example_WithoutFlower_HidesConditionalAnswer()
    {
        DialogueProject project = LoadExample();
        var flags = new DialogueFlagState();
        flags.SetFlag("has_flower", false);
        var session = new DialogueSession(project.Dialogue, flags);

        DialogueChoice promise = session.GetAvailableChoices()
            .Single(choice => choice.Text == "Обещаю помочь");
        Assert.IsTrue(session.TryChoose(promise.Id, out var changes));
        foreach (DialogueFlagChange change in changes)
            flags.SetFlag(change.FlagName, change.NewValue);

        Assert.IsTrue(session.TryContinue());
        List<DialogueChoice> choices = session.GetAvailableChoices();
        Assert.HasCount(1, choices);
        Assert.AreEqual("Пока нет", choices[0].Text);
        Assert.IsTrue(session.TryChoose(choices[0].Id));
        Assert.AreEqual("Возвращайся, когда найдёшь цветок.", session.GetCurrentNode()?.Text);
    }
}
