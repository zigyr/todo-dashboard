using System.Collections.ObjectModel;
using TodoDashboard.Models;
using TodoDashboard.Services;

namespace TodoDashboard.ViewModels;

public sealed class ActionsViewModel : ObservableObject
{
    private readonly ActionRepository _repository;
    private readonly Func<ActionRecord, Task<string?>> _requestDropReason;
    private readonly List<ActionRecord> _all = new();

    private string _selectedSort = "推荐";
    private ActionRecord? _currentAction;
    private bool _isBusy;
    private string? _notice;
    private int _nextCount;
    private int _doingCount;
    private int _doneCount;
    private int _droppedCount;

    public ActionsViewModel(
        ActionRepository repository,
        Func<ActionRecord, Task<string?>> requestDropReason)
    {
        _repository = repository;
        _requestDropReason = requestDropReason;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
        StartActionCommand = new AsyncRelayCommand<ActionRecord>(StartAsync, _ => !IsBusy);
        CompleteActionCommand = new AsyncRelayCommand<ActionRecord>(CompleteAsync, _ => !IsBusy);
        DropActionCommand = new AsyncRelayCommand<ActionRecord>(DropAsync, _ => !IsBusy);
        GoCaptureCommand = new RelayCommand(_ => CaptureRequested?.Invoke(this, EventArgs.Empty));
        SortOptions = new[] { "推荐", "由易到难", "最新" };
    }

    public ObservableCollection<ActionRecord> Actions { get; } = new();
    public IReadOnlyList<string> SortOptions { get; }
    public string WorkspacePath => _repository.Root;

    public string SelectedSort
    {
        get => _selectedSort;
        set
        {
            if (SetProperty(ref _selectedSort, value))
            {
                ApplySort();
            }
        }
    }

    public ActionRecord? CurrentAction
    {
        get => _currentAction;
        private set
        {
            if (SetProperty(ref _currentAction, value))
            {
                OnPropertyChanged(nameof(HasCurrentAction));
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                RefreshCommand.RaiseCanExecuteChanged();
                StartActionCommand.RaiseCanExecuteChanged();
                CompleteActionCommand.RaiseCanExecuteChanged();
                DropActionCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string? Notice
    {
        get => _notice;
        private set
        {
            if (SetProperty(ref _notice, value))
            {
                OnPropertyChanged(nameof(HasNotice));
            }
        }
    }

    public int NextCount
    {
        get => _nextCount;
        private set => SetProperty(ref _nextCount, value);
    }

    public int DoingCount
    {
        get => _doingCount;
        private set => SetProperty(ref _doingCount, value);
    }

    public int DoneCount
    {
        get => _doneCount;
        private set => SetProperty(ref _doneCount, value);
    }

    public int DroppedCount
    {
        get => _droppedCount;
        private set => SetProperty(ref _droppedCount, value);
    }

    public bool HasCurrentAction => CurrentAction is not null;
    public bool HasNotice => !string.IsNullOrWhiteSpace(Notice);
    public bool HasActions => Actions.Count > 0;
    public bool HasNoActions => !HasActions;
    public string EmptyText => "还没有行动。先去「捕获」一篇文章，用 AI 拆解后保存。";

    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand<ActionRecord> StartActionCommand { get; }
    public AsyncRelayCommand<ActionRecord> CompleteActionCommand { get; }
    public AsyncRelayCommand<ActionRecord> DropActionCommand { get; }
    public RelayCommand GoCaptureCommand { get; }

    public event EventHandler? CaptureRequested;

    public Task InitializeAsync() => RefreshAsync();

    private async Task RefreshAsync()
    {
        IsBusy = true;
        try
        {
            var items = await _repository.ListAsync();
            _all.Clear();
            _all.AddRange(items);
            ApplySort();
        }
        catch (Exception error)
        {
            Notice = "读取行动失败：" + error.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplySort()
    {
        IEnumerable<ActionRecord> sorted = SelectedSort switch
        {
            "由易到难" => _all
                .OrderBy(action => action.EffortMinutes)
                .ThenByDescending(action => action.Priority),
            "最新" => _all.OrderByDescending(action => action.CreatedAt, StringComparer.Ordinal),
            _ => _all
                .OrderByDescending(action => action.Priority)
                .ThenBy(action => action.EffortMinutes)
                .ThenBy(action => action.CreatedAt, StringComparer.Ordinal),
        };

        Actions.Clear();
        foreach (var action in sorted)
        {
            Actions.Add(action);
        }

        NextCount = _all.Count(action => action.IsNext);
        DoingCount = _all.Count(action => action.IsDoing);
        DoneCount = _all.Count(action => action.IsDone);
        DroppedCount = _all.Count(action => action.IsDropped);

        CurrentAction = Actions.FirstOrDefault(action => action.IsNext)
            ?? Actions.FirstOrDefault(action => action.IsDoing);

        OnPropertyChanged(nameof(HasActions));
        OnPropertyChanged(nameof(HasNoActions));

        if (DoingCount >= 3)
        {
            Notice = "进行中已经有 3 条，建议先完成一件，再开新的。";
        }
        else if (Notice?.StartsWith("进行中", StringComparison.Ordinal) == true)
        {
            Notice = null;
        }
    }

    private async Task StartAsync(ActionRecord action)
    {
        await _repository.UpdateStatusAsync(action, "doing", null);
        await RefreshAsync();
        Notice = $"已开始：{action.Title}";
    }

    private async Task CompleteAsync(ActionRecord action)
    {
        await _repository.UpdateStatusAsync(action, "done", null);
        await RefreshAsync();
        Notice = $"已完成：{action.Title}";
    }

    private async Task DropAsync(ActionRecord action)
    {
        var reason = await _requestDropReason(action);
        if (string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        await _repository.UpdateStatusAsync(action, "dropped", reason);
        await RefreshAsync();
        Notice = $"已放弃：{action.Title}";
    }
}
