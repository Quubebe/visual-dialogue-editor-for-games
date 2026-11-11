using System.IO;
using Dialogue.Core;
using Dialogue.Persistence;

namespace Dialogue.Tests;

[TestClass]
public sealed class DialogueProjectFileStorageTests
{
    [TestMethod]
    public void SaveAndLoad_PreservesDialogueAndUpdatedPosition()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"dialogue-project-{Guid.NewGuid():N}.json");

        try
        {
            var node = new DialogueNode { Text = "Начало", IsEnding = true };
            var project = new DialogueProject();
            project.Dialogue.Title = "Первая версия";
            project.Dialogue.StartNodeId = node.Id;
            project.Dialogue.Nodes.Add(node);
            project.NodePositions.Add(new NodePosition
            {
                NodeId = node.Id,
                X = 10,
                Y = 20
            });

            DialogueProjectFileStorage.Save(path, project);
            Assert.IsTrue(File.Exists(path));

            DialogueProject loaded = DialogueProjectFileStorage.Load(path);
            Assert.AreEqual(project.Dialogue.Id, loaded.Dialogue.Id);
            Assert.AreEqual(node.Id, loaded.NodePositions[0].NodeId);
            Assert.AreEqual(10.0, loaded.NodePositions[0].X);

            project.Dialogue.Title = "После правки";
            project.NodePositions[0].X = 25;
            DialogueProjectFileStorage.Save(path, project);

            loaded = DialogueProjectFileStorage.Load(path);
            Assert.AreEqual("После правки", loaded.Dialogue.Title);
            Assert.AreEqual(25.0, loaded.NodePositions[0].X);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}