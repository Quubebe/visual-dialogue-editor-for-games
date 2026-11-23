using System.ComponentModel;
using Dialogue.Core;
using System.Collections.ObjectModel;
using Dialogue.Persistence;

namespace Dialogue.Editor;

public class NodeViewModel : INotifyPropertyChanged
{
    public DialogueNode Model { get; }
    public ObservableCollection<ChoiceViewModel> Choices { get; }

    public Guid Id => Model.Id;

    private bool _isStart;

    public bool IsStart
    {
        get => _isStart;
        set
        {
            if (_isStart == value) return;

            _isStart = value;
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(IsStart)));
        }
    }
    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;

            _isSelected = value;
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }
    private readonly ObservableCollection<CharacterViewModel> _characters;

    public CharacterViewModel? Speaker =>
        _characters.FirstOrDefault(character => character.Id == CharacterId);

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

    public NodeViewModel(DialogueNode model, NodePosition? position, ObservableCollection<CharacterViewModel> characters)
    {
        _characters = characters ?? throw new ArgumentNullException(nameof(characters));
        Model = model ?? throw new ArgumentNullException(nameof(model));
        Position = position ?? new NodePosition { NodeId = model.Id };
        Choices = new ObservableCollection<ChoiceViewModel>();

        foreach (DialogueChoice choice in model.Choices)
            Choices.Add(new ChoiceViewModel(choice));
    }

    public DialogueFlagCondition? ShowCondition => Model.ShowCondition;

    public bool HasShowCondition
    {
        get => Model.ShowCondition is not null;
        set
        {
            if (value == HasShowCondition)
                return;

            Model.ShowCondition = value
                ? new DialogueFlagCondition()
                : null;

            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(HasShowCondition)));
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(ShowCondition)));
        }
    }

    public Guid? CharacterId
    {
        get => Model.CharacterId;
        set
        {
            if (Model.CharacterId == value) return;

            Model.CharacterId = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CharacterId)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Speaker)));
        }
    }

    public Guid? NextNodeId
    {
        get => Model.NextNodeId;
        set
        {
            if (Model.NextNodeId == value) return;

            Model.NextNodeId = value;
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(NextNodeId)));
        }
    }

    public NodePosition Position { get; }

    public double X
    {
        get => Position.X;
        set
        {
            if (Position.X == value) return;
            Position.X = value;
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(nameof(X)));
        }
    }

    public double Y
    {
        get => Position.Y;
        set
        {
            if (Position.Y == value) return;
            Position.Y = value;
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(nameof(Y)));
        }
    }
}