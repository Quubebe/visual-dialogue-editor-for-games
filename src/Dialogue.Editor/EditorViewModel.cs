using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Input;
using Dialogue.Core;
using Dialogue.Persistence;

namespace Dialogue.Editor;

public class EditorViewModel : INotifyPropertyChanged
{
    private NodeViewModel? _selectedNode;

    public DialogueProject Project { get; private set;} = new();

    public void SaveProject(string path)
    {
        DialogueProjectFileStorage.Save(path, Project);
    }

    public void LoadProject(string path)
    {
        DialogueProject loaded = DialogueProjectFileStorage.Load(path);

        if(loaded.Dialogue.Nodes is null ||
        loaded.Dialogue.Nodes.Any(node=> node is null ||
        node.Choices is null))
        {
            throw new InvalidDataException("В проекте поврежден список реплик или ответов.");
        }

        Project = loaded;
        SelectedNode = null;
        Nodes.Clear();

        foreach(DialogueNode node in Project.Dialogue.Nodes) Nodes.Add(new NodeViewModel(node));

        SelectedNode = Nodes.FirstOrDefault(node=>node.Id == Project.Dialogue.StartNodeId) ?? Nodes.FirstOrDefault();
    }

    public ObservableCollection<NodeViewModel> Nodes { get; } = new();
    public ICommand AddNodeCommand { get; }
    public ICommand AddChoiceCommand { get; }

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
        AddChoiceCommand = new RelayCommand(AddChoice);
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

    private void AddChoice()
    {
        if (SelectedNode is null) return;

        var choice = new DialogueChoice { Text = "Новый ответ" };

        SelectedNode.Model.Choices.Add(choice);
        SelectedNode.Choices.Add(choice);
        SelectedNode.IsEnding = false;
    }
}