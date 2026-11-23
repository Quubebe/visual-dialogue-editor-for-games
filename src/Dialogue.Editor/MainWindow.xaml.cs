using System.Windows;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows.Controls;
using Dialogue.Core;
using Dialogue.Persistence;

namespace Dialogue.Editor;

public partial class MainWindow : Window
{
    private string? _currentProjectPath;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = new EditorViewModel();
    }

    private void SaveProject_Click(object sender, RoutedEventArgs e)
    {
        SaveProject();
    }

    private void SaveAsProject_Click(object sender, RoutedEventArgs e)
    {
        SaveProject(choosePath: true);
    }

    private bool SaveProject(bool choosePath = false)
    {
        if (DataContext is not EditorViewModel editor)
            return false;

        string? path = choosePath ? null : _currentProjectPath;
        if (path is null)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Сохранить проект диалога",
                Filter = "Проект диалога (*.dproj)|*.dproj",
                DefaultExt = ".dproj",
                AddExtension = true,
                FileName = _currentProjectPath is null
                    ? "Новый диалог.dproj"
                    : System.IO.Path.GetFileName(_currentProjectPath)
            };

            if (dialog.ShowDialog(this) != true)
                return false;

            path = dialog.FileName;
        }

        try
        {
            editor.SaveProject(path);
            _currentProjectPath = path;
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Не удалось сохранить проект",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private bool ConfirmUnsavedChanges()
    {
        if (DataContext is not EditorViewModel editor || !editor.HasUnsavedChanges)
            return true;

        MessageBoxResult answer = MessageBox.Show(
            this,
            "Сохранить изменения в текущем проекте?",
            "Несохранённые изменения",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);

        return answer switch
        {
            MessageBoxResult.Yes => SaveProject(),
            MessageBoxResult.No => true,
            _ => false
        };
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!ConfirmUnsavedChanges())
            e.Cancel = true;

        base.OnClosing(e);
    }

    private void OpenProject_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Открыть проект диалога",
            Filter = "Проект диалога (*.dproj)|*.dproj"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        if (!ConfirmUnsavedChanges())
            return;

        try
        {
            ((EditorViewModel)DataContext).LoadProject(dialog.FileName);
            _currentProjectPath = dialog.FileName;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Не удалось открыть проект",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        var editor = (EditorViewModel)DataContext;
        var preview = new PreviewWindow(editor.Project.Dialogue)
        {
            Owner = this
        };
        preview.ShowDialog();
    }

    private void ValidationIssues_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox { SelectedItem: DialogueValidationIssue issue } ||
            issue.NodeId is not Guid nodeId ||
            DataContext is not EditorViewModel editor)
            return;

        editor.SelectedNode = editor.Nodes.FirstOrDefault(node => node.Id == nodeId);
    }

    private void Graph_Click(object sender, RoutedEventArgs e)
    {
        var editor = (EditorViewModel)DataContext;
        var graph = new GraphWindow(editor) { Owner = this };
        graph.Show();
    }
    private void DeleteNode_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not EditorViewModel editor)
            return;

        if (editor.SelectedNode is { } node &&
            MessageBox.Show(
                this,
                $"Удалить реплику «{node.Text}»?",
                "Удаление реплики",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        if (!editor.TryDeleteSelectedNode(out string error))
            MessageBox.Show(this, error, "Удаление реплики",
                MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportDialogue_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not EditorViewModel editor)
            return;

        editor.ValidateCommand.Execute(null);

        if (editor.ValidationIssues.Any(issue => issue.IsError))
        {
            MessageBox.Show(this,
                "Исправьте ошибки из списка «Последняя проверка» перед экспортом.",
                "Экспорт диалога",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Экспорт диалога",
            Filter = "Диалог JSON (*.json)|*.json",
            DefaultExt = ".json",
            AddExtension = true,
            FileName = "dialogue.json"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            DialogueFileStorage.Save(dialog.FileName, editor.Project.Dialogue);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Не удалось экспортировать диалог",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
