using System.Text.Json;
using Dialogue.Core;
using Dialogue.Persistence;
using System.IO;

namespace Dialogue.Tests;

[TestClass]
public sealed class DialogueJsonSerializerTests
{
    [TestMethod]
    public void Serialize_PreservesIdsAndTransition()
    {
        var start = new DialogueNode { Text = "Начало" };
        var end = new DialogueNode { Text = "Конец", IsEnding = true };
        var choice = new DialogueChoice { Text = "Дальше", TargetNodeId = end.Id };
        start.Choices.Add(choice);

        var dialogue = new DialogueDocument { StartNodeId = start.Id };
        dialogue.Nodes.AddRange(new[] { start, end });

        string json = DialogueJsonSerializer.Serialize(dialogue);
        using JsonDocument parsed = JsonDocument.Parse(json);

        JsonElement root = parsed.RootElement;
        JsonElement savedChoice = root.GetProperty("Nodes")[0].GetProperty("Choices")[0];

        Assert.AreEqual(dialogue.Id.ToString(), root.GetProperty("Id").GetString());
        Assert.AreEqual(1, root.GetProperty("FormatVersion").GetInt32());
        Assert.AreEqual(start.Id.ToString(), root.GetProperty("StartNodeId").GetString());
        Assert.AreEqual(choice.Id.ToString(), savedChoice.GetProperty("Id").GetString());
        Assert.AreEqual(end.Id.ToString(), savedChoice.GetProperty("TargetNodeId").GetString());
    }

    [TestMethod]
    public void Deserialize_RestoresDialogueAndLinks()
    {
        var character = new DialogueCharacter { Name = "Страж" };
        var start = new DialogueNode
        {
            CharacterId = character.Id,
            Text = "Куда идёшь?"
        };
        var end = new DialogueNode { Text = "До встречи.", IsEnding = true };
        var choice = new DialogueChoice { Text = "Уйти", TargetNodeId = end.Id };
        start.Choices.Add(choice);

        var original = new DialogueDocument
        {
            Title = "У ворот",
            StartNodeId = start.Id
        };
        original.Characters.Add(character);
        original.Nodes.AddRange(new[] { start, end });

        string json = DialogueJsonSerializer.Serialize(original);
        DialogueDocument restored = DialogueJsonSerializer.Deserialize(json);

        Assert.AreEqual(original.Id, restored.Id);
        Assert.AreEqual(original.Title, restored.Title);
        Assert.AreEqual(start.Id, restored.StartNodeId);
        Assert.AreEqual(character.Id, restored.Characters[0].Id);
        Assert.AreEqual(choice.Id, restored.Nodes[0].Choices[0].Id);
        Assert.AreEqual<Guid?>(end.Id, restored.Nodes[0].Choices[0].TargetNodeId);
    }

    [TestMethod]
    public void Deserialize_RejectsMissingAndUnsupportedVersion()
    {
        Assert.ThrowsExactly<InvalidDataException>(
            () => DialogueJsonSerializer.Deserialize("{}"));

        var future = new DialogueDocument { FormatVersion = 2 };
        string futureJson = DialogueJsonSerializer.Serialize(future);

        InvalidDataException error = Assert.ThrowsExactly<InvalidDataException>(
            () => DialogueJsonSerializer.Deserialize(futureJson));

        Assert.AreEqual("Версия формата 2 не поддерживается.", error.Message);
    }

    [TestMethod]
    public void Deserialize_ExplainsEmptyAndMalformedJson()
    {
        InvalidDataException empty = Assert.ThrowsExactly<InvalidDataException>(
            () => DialogueJsonSerializer.Deserialize(""));
        Assert.AreEqual("JSON диалога пуст.", empty.Message);

        InvalidDataException malformed = Assert.ThrowsExactly<InvalidDataException>(
            () => DialogueJsonSerializer.Deserialize("{"));
        Assert.AreEqual("Не удалось прочитать JSON диалога.", malformed.Message);
    }

    [TestMethod]
    public void ProjectJson_RestoresConditionsAndFlagChanges()
    {
        var node = new DialogueNode
        {
            ShowCondition = new DialogueFlagCondition
            {
                FlagName = "quest",
                ExpectedValue = false
            }
        };

        var choice = new DialogueChoice
        {
            ShowCondition = new DialogueFlagCondition
            {
                FlagName = "quest",
                ExpectedValue = false
            }
        };
        choice.FlagChanges.Add(new DialogueFlagChange
        {
            FlagName = "quest",
            NewValue = true
        });
        node.Choices.Add(choice);

        var project = new DialogueProject();
        project.Dialogue.Nodes.Add(node);

        string json = DialogueProjectJsonSerializer.Serialize(project);
        DialogueProject restored = DialogueProjectJsonSerializer.Deserialize(json);

        var restoredNode = restored.Dialogue.Nodes[0];
        var restoredChoice = restoredNode.Choices[0];

        Assert.IsNotNull(restoredNode.ShowCondition);
        Assert.AreEqual("quest", restoredNode.ShowCondition.FlagName);
        Assert.IsFalse(restoredNode.ShowCondition.ExpectedValue);

        Assert.IsNotNull(restoredChoice.ShowCondition);
        Assert.AreEqual("quest", restoredChoice.ShowCondition.FlagName);
        Assert.IsFalse(restoredChoice.ShowCondition.ExpectedValue);

        Assert.HasCount(1, restoredChoice.FlagChanges);
        Assert.AreEqual("quest", restoredChoice.FlagChanges[0].FlagName);
        Assert.IsTrue(restoredChoice.FlagChanges[0].NewValue);
    }
}
