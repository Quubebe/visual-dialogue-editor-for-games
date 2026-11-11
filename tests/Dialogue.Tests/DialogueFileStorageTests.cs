using System.IO;
using Dialogue.Core;
using Dialogue.Persistence;

namespace Dialogue.Tests;

[TestClass]
public sealed class DialogueFileStorageTests
{
    [TestMethod]
    public void SaveAndLoad_CreatesAndReplacesDialogueFile()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"dialogue-{Guid.NewGuid():N}.json");

        try
        {
            var start = new DialogueNode { Text = "Первая реплика", IsEnding = true };
            var document = new DialogueDocument
            {
                Title = "Первая версия",
                StartNodeId = start.Id
            };
            document.Nodes.Add(start);

            DialogueFileStorage.Save(path, document);
            Assert.IsTrue(File.Exists(path));

            DialogueDocument loaded = DialogueFileStorage.Load(path);
            Assert.AreEqual(document.Id, loaded.Id);
            Assert.AreEqual(start.Id, loaded.StartNodeId);
            Assert.AreEqual("Первая реплика", loaded.Nodes[0].Text);

            document.Title = "Вторая версия";
            start.Text = "Изменённая реплика";
            DialogueFileStorage.Save(path, document);

            loaded = DialogueFileStorage.Load(path);
            Assert.AreEqual("Вторая версия", loaded.Title);
            Assert.AreEqual("Изменённая реплика", loaded.Nodes[0].Text);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
