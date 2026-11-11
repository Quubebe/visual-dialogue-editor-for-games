using System.IO;
using Dialogue.Core;

namespace Dialogue.Persistence;

public static class DialogueFileStorage
{
    public static DialogueDocument Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Путь к файлу диалога не указан.", nameof(path));

        string json;


        try
        {
            json = File.ReadAllText(path);
        }
        catch (IOException ex)
        {
            throw new IOException($"Не удалось прочитать файл диалога '{path}'.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new IOException($"Нет доступа к файлу диалога '{path}'.", ex);
        }
        return DialogueJsonSerializer.Deserialize(json);
    }

    public static void Save(string path, DialogueDocument document)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Путь к файлу диалога не указан.", nameof(path));

        ArgumentNullException.ThrowIfNull(document);

        AtomicTextFile.Write(path, DialogueJsonSerializer.Serialize(document));
    }
}
