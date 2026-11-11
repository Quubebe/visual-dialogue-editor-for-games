using System.IO;
using System.Text.Json;

namespace Dialogue.Persistence;

public static class DialogueProjectJsonSerializer
{
    public static string Serialize(DialogueProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        return JsonSerializer.Serialize(project, new JsonSerializerOptions { WriteIndented = true });
    }

    public static DialogueProject Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new InvalidDataException("JSON проекта пуст.");

        try
        {
            using JsonDocument parsed = JsonDocument.Parse(json);
            JsonElement root = parsed.RootElement;

            if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("ProjectFormatVersion", out JsonElement version) ||
            version.ValueKind != JsonValueKind.Number ||
            !version.TryGetInt32(out int formatVersion))
            {
                throw new InvalidDataException("В JSON проекта отсутствует корректная версия формата.");
            }

            if (formatVersion != 1) throw new InvalidDataException($"Версия формата проекта {formatVersion} не поддерживается.");

            if (!root.TryGetProperty("Dialogue", out JsonElement dialogueElement) ||
            dialogueElement.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("NodePositions", out JsonElement positionsElement) ||
            positionsElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException("В JSON проекта отсутствуют данные диалога или расположение карточек.");
            }

            DialogueProject project = JsonSerializer.Deserialize<DialogueProject>(json)
            ?? throw new InvalidDataException("JSON не содержит проект.");

            project.Dialogue = DialogueJsonSerializer.Deserialize(dialogueElement.GetRawText());

            return project;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Не удалось проситать JSON проекта.", ex);
        }
    }
}
