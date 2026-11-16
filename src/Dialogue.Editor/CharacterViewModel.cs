using System.ComponentModel;
using Dialogue.Core;

namespace Dialogue.Editor;

public class CharacterViewModel : INotifyPropertyChanged
{
    public DialogueCharacter Model { get; }

    public CharacterViewModel(DialogueCharacter model)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public Guid Id => Model.Id;

    public string Name
    {
        get => Model.Name;
        set
        {
            if (Model.Name == value) return;

            Model.Name = value;
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(Name)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}