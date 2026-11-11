using System.Text.Json;
using Dialogue.Core;
using System.IO;

namespace Dialogue.Persistence;

public static class DialogueJsonSerializer
{
    public static string Serialize(DialogueDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true });
    }

    public static DialogueDocument Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new InvalidDataException("JSON диалога пуст.");

        try
        {
            using JsonDocument parsed = JsonDocument.Parse(json);

            if (parsed.RootElement.ValueKind != JsonValueKind.Object ||
            !parsed.RootElement.TryGetProperty("FormatVersion", out JsonElement versionElement) ||
            versionElement.ValueKind != JsonValueKind.Number ||
            !versionElement.TryGetInt32(out int version))
            {
                throw new InvalidDataException("В JSON диалога отсутствует корректная версия формата.");
            }

            if (version != 1) throw new InvalidDataException($"Версия формата {version} не поддерживается.");

            return JsonSerializer.Deserialize<DialogueDocument>(json)
            ?? throw new InvalidDataException("JSON не содержит документ диалога.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Не удалось прочитать JSON диалога.", ex);
        }
    }
}
