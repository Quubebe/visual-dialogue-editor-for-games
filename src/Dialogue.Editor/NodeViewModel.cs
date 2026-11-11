using System.ComponentModel;
using Dialogue.Core;

namespace Dialogue.Editor;

public class NodeViewModel : INotifyPropertyChanged
{
    public DialogueNode Model { get; }

    public Guid Id => Model.Id;

    public string Text
    {
        get => Model.Text;
        set
        {
            if (Model.Text == value)
                return;

            Model.Text = value;
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(Text)));
        }
    }

    public bool IsEnding
    {
        get => Model.IsEnding;
        set
        {
            if (Model.IsEnding == value)
                return;

            Model.IsEnding = value;
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(IsEnding)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public NodeViewModel(DialogueNode model)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
    }
}