using System.ComponentModel;
using Dialogue.Core;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Dialogue.Editor;

public class ChoiceViewModel : INotifyPropertyChanged
{
    public ObservableCollection<DialogueFlagChange> FlagChanges { get; }
    public ICommand AddFlagChangeCommand { get; }
    public DialogueChoice Model { get; }
    public ICommand RemoveFlagChangeCommand { get; }
    public ChoiceViewModel(DialogueChoice model)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        FlagChanges = new ObservableCollection<DialogueFlagChange>(model.FlagChanges);
        AddFlagChangeCommand = new RelayCommand(AddFlagChange);
        RemoveFlagChangeCommand = new RelayCommand(RemoveFlagChange);
    }

    public string Text
    {
        get => Model.Text;
        set
        {
            if (Model.Text == value) return;
            Model.Text = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
        }
    }

    public Guid? TargetNodeId
    {
        get => Model.TargetNodeId;
        set
        {
            if (Model.TargetNodeId == value) return;
            Model.TargetNodeId = value;
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(nameof(TargetNodeId)));
        }
    }

    public DialogueFlagCondition? ShowCondition => Model.ShowCondition;

    public bool HasShowCondition
    {
        get => Model.ShowCondition is not null;
        set
        {
            if (value == HasShowCondition) return;

            Model.ShowCondition = value
                ? new DialogueFlagCondition()
                : null;

            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(nameof(HasShowCondition)));
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(nameof(ShowCondition)));
        }
    }

    private void AddFlagChange()
    {
        var change = new DialogueFlagChange();

        Model.FlagChanges.Add(change); // Данные для сохранения
        FlagChanges.Add(change);       // Данные для обновления окна
    }

    private void RemoveFlagChange(object? parameter)
    {
        if (parameter is not DialogueFlagChange change)
            return;

        Model.FlagChanges.Remove(change);
        FlagChanges.Remove(change);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
