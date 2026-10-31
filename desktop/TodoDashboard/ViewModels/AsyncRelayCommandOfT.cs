using System.Diagnostics;
using System.Windows.Input;

namespace TodoDashboard.ViewModels;

public sealed class AsyncRelayCommand<T> : ICommand
{
    private readonly Func<T, Task> _execute;
    private readonly Func<T, bool>? _canExecute;
    private bool _isRunning;

    public AsyncRelayCommand(Func<T, Task> execute, Func<T, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return !_isRunning
            && parameter is T value
            && (_canExecute?.Invoke(value) ?? true);
    }

    public async void Execute(object? parameter)
    {
        if (parameter is not T value || !CanExecute(parameter))
        {
            return;
        }

        _isRunning = true;
        RaiseCanExecuteChanged();

        try
        {
            await _execute(value);
        }
        catch (Exception error)
        {
            Debug.WriteLine(error);
        }
        finally
        {
            _isRunning = false;
            RaiseCanExecuteChanged();
        }
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
