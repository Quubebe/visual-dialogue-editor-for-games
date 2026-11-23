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
    private string _savedProjectSnapshot = string.Empty;

    public DialogueProject Project { get; private set; } = new();
    public bool HasUnsavedChanges =>
        DialogueProjectJsonSerializer.Serialize(Project) != _savedProjectSnapshot;

    public void SaveProject(string path)
    {
        DialogueProjectFileStorage.Save(path, Project);
        _savedProjectSnapshot = DialogueProjectJsonSerializer.Serialize(Project);
    }

    public void LoadProject(string path)
    {
        DialogueProject loaded = DialogueProjectFileStorage.Load(path);

        if (loaded.Dialogue.Nodes is null ||
        loaded.Dialogue.Nodes.Any(node => node is null ||
        node.Choices is null))
        {
            throw new InvalidDataException("В проекте поврежден список реплик или ответов.");
        }

        Project = loaded;
        ValidationIssues.Clear();
        SelectedNode = null;
        Nodes.Clear();
        Characters.Clear();
        foreach (DialogueCharacter character in Project.Dialogue.Characters) Characters.Add(new CharacterViewModel(character));

        foreach (DialogueNode node in Project.Dialogue.Nodes) Nodes.Add(CreateNodeItem(node));
        UpdateStartFlags();
        SelectedNode = Nodes.FirstOrDefault(node => node.Id == Project.Dialogue.StartNodeId) ?? Nodes.FirstOrDefault();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartNode)));
        _savedProjectSnapshot = DialogueProjectJsonSerializer.Serialize(Project);
    }

    public ObservableCollection<NodeViewModel> Nodes { get; } = new();
    public ICommand AddNodeCommand { get; }
    public ICommand AddChoiceCommand { get; }
    public ObservableCollection<DialogueValidationIssue> ValidationIssues { get; } = new();
    public ICommand ValidateCommand { get; }
    public NodeViewModel? StartNode => Nodes.FirstOrDefault(node => node.Id == Project.Dialogue.StartNodeId);
    public ICommand SetStartNodeCommand { get; }
    public ObservableCollection<CharacterViewModel> Characters { get; } = new();
    public ICommand AddCharacterCommand { get; }
    public ICommand ClearCharacterCommand { get; }
    public ICommand RemoveChoiceCommand { get; }
    public ICommand ClearNextNodeCommand { get; }

    public NodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (_selectedNode == value)
                return;

            if (_selectedNode is not null)
                _selectedNode.IsSelected = false;

            _selectedNode = value;

            if (_selectedNode is not null)
                _selectedNode.IsSelected = true;
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

        var firstItem = CreateNodeItem(first);
        Nodes.Add(firstItem);
        UpdateStartFlags();
        SelectedNode = firstItem;

        ValidateCommand = new RelayCommand(Validate);
        SetStartNodeCommand = new RelayCommand(SetStartNode);
        AddCharacterCommand = new RelayCommand(AddCharacter);
        ClearCharacterCommand = new RelayCommand(() =>
        {
            if (SelectedNode is not null) SelectedNode.CharacterId = null;
        });
        RemoveChoiceCommand = new RelayCommand(RemoveChoice);

        ClearNextNodeCommand = new RelayCommand(() =>
        {
            if (SelectedNode is not null)
                SelectedNode.NextNodeId = null;
        });

        _savedProjectSnapshot = DialogueProjectJsonSerializer.Serialize(Project);
    }

    private void AddNode()
    {
        var node = new DialogueNode
        {
            Text = "Новая реплика",
            IsEnding = true
        };

        Project.Dialogue.Nodes.Add(node);

        var item = CreateNodeItem(node);
        Nodes.Add(item);
        SelectedNode = item;
    }
    public void AddNodeAt(double x, double y)
    {
        AddNode();

        if (SelectedNode is null)
            return;

        SelectedNode.X = Math.Max(0, x);
        SelectedNode.Y = Math.Max(0, y);
    }

    private void AddChoice()
    {
        if (SelectedNode is not null)
            AddChoiceTo(SelectedNode, null);
    }

    public void AddChoiceTo(NodeViewModel source, Guid? targetNodeId)
    {
        var choice = new DialogueChoice
        {
            Text = "Новый ответ",
            TargetNodeId = targetNodeId
        };

        source.Model.Choices.Add(choice);
        source.Choices.Add(new ChoiceViewModel(choice));
        source.IsEnding = false;
    }

    private void Validate()
    {
        ValidationIssues.Clear();
        foreach (DialogueValidationIssue issue in DialogueValidator.Validate(Project.Dialogue))
        {
            ValidationIssues.Add(issue);
        }
    }

    private void SetStartNode()
    {
        if (SelectedNode is null) return;

        Project.Dialogue.StartNodeId = SelectedNode.Id;
        UpdateStartFlags();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartNode)));
    }

    private void AddCharacter()
    {
        var character = new DialogueCharacter { Name = "Новый персонаж" };

        Project.Dialogue.Characters.Add(character);
        Characters.Add(new CharacterViewModel(character));
    }

    private void RemoveChoice(object? parameter)
    {
        if (SelectedNode is null ||
            parameter is not ChoiceViewModel choice)
            return;

        SelectedNode.Model.Choices.Remove(choice.Model);
        SelectedNode.Choices.Remove(choice);
    }

    private NodeViewModel CreateNodeItem(DialogueNode node)
    {
        NodePosition? position = Project.NodePositions.Find(
            item => item.NodeId == node.Id);

        if (position is null)
        {
            position = new NodePosition
            {
                NodeId = node.Id,
                X = 40 + (Nodes.Count % 3) * 240,
                Y = 40 + (Nodes.Count / 3) * 160
            };
            Project.NodePositions.Add(position);
        }

        return new NodeViewModel(node, position, Characters);
    }
    private void UpdateStartFlags()
    {
        foreach (var node in Nodes)
            node.IsStart = node.Id == Project.Dialogue.StartNodeId;
    }
    public bool TryDeleteSelectedNode(out string error)
    {
        if (SelectedNode is not { } node)
        {
            error = "Выберите реплику.";
            return false;
        }

        if (Nodes.Count == 1)
        {
            error = "Нельзя удалить единственную реплику.";
            return false;
        }

        if (node.Id == Project.Dialogue.StartNodeId)
        {
            error = "Сначала назначьте другую стартовую реплику.";
            return false;
        }

        bool hasIncomingLinks = Nodes.Any(other =>
            other != node &&
            (other.NextNodeId == node.Id ||
             other.Choices.Any(choice => choice.TargetNodeId == node.Id)));

        if (hasIncomingLinks)
        {
            error = "На эту реплику ведёт переход. Сначала измените или удалите его.";
            return false;
        }

        SelectedNode = null;
        Project.Dialogue.Nodes.Remove(node.Model);
        Project.NodePositions.Remove(node.Position);
        Nodes.Remove(node);
        ValidationIssues.Clear();
        SelectedNode = StartNode ?? Nodes.FirstOrDefault();

        error = string.Empty;
        return true;
    }
}
