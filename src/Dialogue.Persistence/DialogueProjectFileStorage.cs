using System.IO;

namespace Dialogue.Persistence;

public static class DialogueProjectFileStorage
{
    public static DialogueProject Load(string path)
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
        return DialogueProjectJsonSerializer.Deserialize(json);
    }

    public static void Save(string path, DialogueProject project)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Путь к файлу диалога не указан.", nameof(path));

        ArgumentNullException.ThrowIfNull(project);

        AtomicTextFile.Write(path, DialogueProjectJsonSerializer.Serialize(project));
    }
}
