using System.Windows.Input;

namespace Dialogue.Editor;

public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;

    public RelayCommand(Action execute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        _execute = _ => execute();
    }

    public RelayCommand(Action<object?> execute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    public void Execute(object? parameter) => _execute(parameter);

    public bool CanExecute(object? parameter) => true;

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }
}