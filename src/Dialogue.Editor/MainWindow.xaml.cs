using System.Windows;
using Microsoft.Win32;

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
}