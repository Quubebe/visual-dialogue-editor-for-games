using System.Windows;
using Dialogue.Core;
using System.Windows.Controls;

namespace Dialogue.Editor;

public partial class PreviewWindow : Window
{
    private readonly DialogueDocument _document;
    private DialogueFlagState? _flags;
    private DialogueSession? _session;

    public PreviewWindow(DialogueDocument document)
    {
        InitializeComponent();
        _document = document;
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        _session = null;
        _flags = new DialogueFlagState();
        ChoicesList.ItemsSource = null;
        ContinueButton.Visibility = Visibility.Collapsed;
        StatusText.Text = string.Empty;
        FlagValuesText.Text = string.Empty;

        var names = new HashSet<string>(StringComparer.Ordinal);
        int lineNumber = 0;

        foreach (string rawLine in InitialFlagsBox.Text.Split('\n'))
        {
            lineNumber++;
            string line = rawLine.Trim();
            if (line.Length == 0) continue;
            int separator = line.IndexOf('=');
            if (separator <= 0 || separator == line.Length - 1 || line.IndexOf('=', separator + 1) >= 0)
            {
                PreviewText.Text = $"Строка {lineNumber}: используйте формат имя=true или имя=false.";
                return;
            }

            string name = line[..separator].Trim();
            string valueText = line[(separator + 1)..].Trim();

            if (name.Length == 0 || !bool.TryParse(valueText, out bool value))
            {
                PreviewText.Text = $"Строка {lineNumber}: проверьте имяя и значение true/false.";
                return;
            }

            if (!names.Add(name))
            {
                PreviewText.Text = $"Строка {lineNumber}: флаг {name} указан дважды.";
                return;
            }

            _flags.SetFlag(name, value);
        }
        _session = new DialogueSession(_document, _flags);
        SpeakerText.Text = string.Empty;
        RefreshPreview();
    }

    private void Choose_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null ||
            _flags is null ||
            sender is not Button button ||
            button.Tag is not Guid choiceId)
            return;

        if (!_session.TryChoose(choiceId, out var changes))
        {
            StatusText.Text = "Этот ответ сейчас недоступен.";
            return;
        }

        foreach (DialogueFlagChange change in changes)
            _flags.SetFlag(change.FlagName, change.NewValue);

        RefreshPreview();
    }

    private void RefreshPreview()
    {
        if (_session is null || _flags is null)
            return;

        ContinueButton.Visibility = Visibility.Collapsed;
        var values = _flags.GetSnapshot();
        FlagValuesText.Text = values.Count == 0
            ? "Флаги не заданы."
            : string.Join(Environment.NewLine,
                values.OrderBy(pair => pair.Key)
                      .Select(pair => $"{pair.Key} = {pair.Value}"));

        DialogueNode? node = _session.GetCurrentNode();
        if (node is null)
        {
            PreviewText.Text = "Нет доступной реплики при текущих флагах.";
            ChoicesList.ItemsSource = null;
            StatusText.Text = "Диалог остановлен.";
            SpeakerText.Text = string.Empty;
            return;
        }
        SpeakerText.Text = node.CharacterId is Guid characterId
            ? _document.Characters.Find(character => character.Id == characterId)?.Name
                ?? "Неизвестный персонаж"
            : string.Empty;

        PreviewText.Text = node.Text;
        var choices = _session.GetAvailableChoices();
        ChoicesList.ItemsSource = choices;

        if (!node.IsEnding && node.NextNodeId is not null && node.Choices.Count == 0)
        {
            ContinueButton.Visibility = Visibility.Visible;
        }

        StatusText.Text = node.IsEnding ? "Конец диалога." : node.NextNodeId is not null && node.Choices.Count == 0
        ? string.Empty : choices.Count == 0 ? "Нет допустимых ответов при текущих флагах." : string.Empty;
    }
    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        if (_session is null)
            return;

        if (!_session.TryContinue())
        {
            StatusText.Text = "Следующая реплика сейчас недоступна.";
            return;
        }

        RefreshPreview();
    }
}