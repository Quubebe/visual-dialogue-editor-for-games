using System.Windows;

namespace Dialogue.Editor;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new EditorViewModel();
    }
}