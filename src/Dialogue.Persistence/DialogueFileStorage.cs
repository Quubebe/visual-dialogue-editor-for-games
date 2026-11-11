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
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Путь к файлу диалога не указан.");

        ArgumentNullException.ThrowIfNull(document);

        string json = DialogueJsonSerializer.Serialize(document);
        string fullPath = Path.GetFullPath(path);
        string directory = Path.GetDirectoryName(fullPath)!;
        string temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(temporaryPath, json);
            if (File.Exists(fullPath)) File.Replace(temporaryPath, fullPath, null);
            else File.Move(temporaryPath, fullPath);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
