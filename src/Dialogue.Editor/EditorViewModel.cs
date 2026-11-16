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

    public DialogueProject Project { get; private set; } = new();

    public void SaveProject(string path)
    {
        DialogueProjectFileStorage.Save(path, Project);
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

        foreach (DialogueNode node in Project.Dialogue.Nodes) Nodes.Add(new NodeViewModel(node));

        SelectedNode = Nodes.FirstOrDefault(node => node.Id == Project.Dialogue.StartNodeId) ?? Nodes.FirstOrDefault();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartNode)));
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

        ValidateCommand = new RelayCommand(Validate);
        SetStartNodeCommand = new RelayCommand(SetStartNode);
        AddCharacterCommand = new RelayCommand(AddCharacter);
        ClearCharacterCommand = new RelayCommand(() =>
        {
            if (SelectedNode is not null) SelectedNode.CharacterId = null;
        });
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
        SelectedNode.Choices.Add(new ChoiceViewModel(choice));
        SelectedNode.IsEnding = false;
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
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartNode)));
    }

    private void AddCharacter()
    {
        var character = new DialogueCharacter { Name = "Новый персонаж" };

        Project.Dialogue.Characters.Add(character);
        Characters.Add(new CharacterViewModel(character));
    }
}