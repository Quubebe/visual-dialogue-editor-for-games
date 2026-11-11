using System.IO;
using Dialogue.Core;
using Dialogue.Persistence;

namespace Dialogue.Tests;

[TestClass]
public sealed class DialogueProjectJsonSerializerTests
{
    [TestMethod]
    public void ProjectJson_RestoresDialogueAndPosition()
    {
        var node = new DialogueNode { IsEnding = true };
        var project = new DialogueProject();
        project.Dialogue.StartNodeId = node.Id;
        project.Dialogue.Nodes.Add(node);
        project.NodePositions.Add(new NodePosition
        {
            NodeId = node.Id,
            X = 120,
            Y = 80
        });

        string json = DialogueProjectJsonSerializer.Serialize(project);
        DialogueProject restored = DialogueProjectJsonSerializer.Deserialize(json);

        Assert.AreEqual(project.Dialogue.Id, restored.Dialogue.Id);
        Assert.AreEqual(node.Id, restored.Dialogue.StartNodeId);
        Assert.AreEqual(node.Id, restored.NodePositions[0].NodeId);
        Assert.AreEqual(120.0, restored.NodePositions[0].X);
        Assert.AreEqual(80.0, restored.NodePositions[0].Y);
    }

    [TestMethod]
    public void ProjectJson_RejectsMissingAndUnsupportedVersion()
    {
        Assert.ThrowsExactly<InvalidDataException>(
            () => DialogueProjectJsonSerializer.Deserialize("{}"));

        var future = new DialogueProject { ProjectFormatVersion = 2 };
        string json = DialogueProjectJsonSerializer.Serialize(future);

        InvalidDataException error = Assert.ThrowsExactly<InvalidDataException>(
            () => DialogueProjectJsonSerializer.Deserialize(json));

        Assert.AreEqual(
            "Версия формата проекта 2 не поддерживается.",
            error.Message);
    }
}
