using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Dialogue.Core;
using Dialogue.Persistence;

namespace Dialogue.Editor;

public class EditorViewModel : INotifyPropertyChanged
{
    private NodeViewModel? _selectedNode;

    public DialogueProject Project { get; } = new();
    public ObservableCollection<NodeViewModel> Nodes { get; } = new();
    public ICommand AddNodeCommand { get; }

    public NodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (_selectedNode == value)
                return;

            _selectedNode = value;
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(nameof(SelectedNode)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public EditorViewModel()
    {
        AddNodeCommand = new RelayCommand(AddNode);

        var first = new DialogueNode
        {
            Text = "Первая реплика",
            IsEnding = true
        };

        Project.Dialogue.Title = "Новый диалог";
        Project.Dialogue.StartNodeId = first.Id;
        Project.Dialogue.Nodes.Add(first);

        var firstItem = new NodeViewModel(first);
        Nodes.Add(firstItem);
        SelectedNode = firstItem;
    }

    private void AddNode()
    {
        var node = new DialogueNode
        {
            Text = "Новая реплика",
            IsEnding = true
        };

        Project.Dialogue.Nodes.Add(node);

        var item = new NodeViewModel(node);
        Nodes.Add(item);
        SelectedNode = item;
    }
}