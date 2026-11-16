using System.Windows;
using Microsoft.Win32;
using System.Windows.Controls;
using Dialogue.Core;

namespace Dialogue.Editor;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new EditorViewModel();
    }

    private void SaveProject_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Сохранить проект диалога",
            Filter = "Проект диалога (*.dproj)|*.dproj",
            DefaultExt = ".dproj",
            AddExtension = true,
            FileName = "Новый диалог.dproj"
        };

        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            ((EditorViewModel)DataContext).SaveProject(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Не удалось сохранить проект",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
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

        var answer = MessageBox.Show(
            this,
            "Текущий проект будет заменён. Продолжить?",
            "Открыть проект",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (answer != MessageBoxResult.Yes)
            return;

        try
        {
            ((EditorViewModel)DataContext).LoadProject(dialog.FileName);
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
}