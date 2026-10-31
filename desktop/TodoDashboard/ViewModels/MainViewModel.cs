using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using TodoDashboard.Models;
using TodoDashboard.Services;

namespace TodoDashboard.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly UrlCaptureService _captureService;
    private readonly SourceStore _store;
    private readonly ActionStore _actionStore;
    private readonly DeepSeekService _deepSeek;
    private readonly ApiKeyStore _apiKeyStore;
    private readonly Func<Task<string?>> _requestApiKey;
    private readonly string _workspace;

    private string _url = "";
    private bool _isBusy;
    private bool _isAnalyzing;
    private bool _isTodayView = true;
    private bool _isCaptureView;
    private bool _isReviewView;
    private SourcePreview? _preview;
    private SavedSource? _saved;
    private AnalysisResult? _analysis;
    private string? _errorMessage;
    private string? _analysisError;
    private string? _actionSaveMessage;
    private int _sourceCount;

    public MainViewModel(
        UrlCaptureService captureService,
        SourceStore store,
        ActionStore actionStore,
        DeepSeekService deepSeek,
        ApiKeyStore apiKeyStore,
        string workspace,
        Func<Task<string?>> requestApiKey,
        Func<ActionRecord, Task<string?>> requestDropReason)
    {
        _captureService = captureService;
        _store = store;
        _actionStore = actionStore;
        _deepSeek = deepSeek;
        _apiKeyStore = apiKeyStore;
        _requestApiKey = requestApiKey;
        _workspace = workspace;

        Actions = new ActionsViewModel(new ActionRepository(workspace), requestDropReason);
        Actions.CaptureRequested += (_, _) => IsCaptureView = true;

        CaptureCommand = new AsyncRelayCommand(CaptureAsync, () => CanCapture);
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => HasPreview && !IsBusy);
        AnalyzeCommand = new AsyncRelayCommand(AnalyzeAsync, () => CanAnalyze);
        SaveActionsCommand = new AsyncRelayCommand(SaveActionsAsync, () => CanSaveActions);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
        DiscardCommand = new RelayCommand(_ => Reset());
        ClearAnalysisCommand = new RelayCommand(_ => ClearAnalysis());
        OpenUrlCommand = new RelayCommand(parameter => OpenUrl(parameter as string ?? Preview?.CanonicalUrl));
        OpenWorkspaceCommand = new RelayCommand(_ => OpenWorkspace());

        Recent.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(HasRecent));
            OnPropertyChanged(nameof(HasNoRecent));
            OnPropertyChanged(nameof(RecentEmptyText));
            OnPropertyChanged(nameof(LastCapturedText));
        };
    }

    public ObservableCollection<SourceSummary> Recent { get; } = new();
    public ActionsViewModel Actions { get; }

    public bool IsTodayView
    {
        get => _isTodayView;
        set
        {
            if (!SetProperty(ref _isTodayView, value))
            {
                return;
            }

            if (value)
            {
                _isCaptureView = false;
                _isReviewView = false;
                OnPropertyChanged(nameof(IsCaptureView));
                OnPropertyChanged(nameof(IsReviewView));
                _ = Actions.InitializeAsync();
            }
        }
    }

    public bool IsCaptureView
    {
        get => _isCaptureView;
        set
        {
            if (!SetProperty(ref _isCaptureView, value))
            {
                return;
            }

            if (value)
            {
                _isTodayView = false;
                _isReviewView = false;
                OnPropertyChanged(nameof(IsTodayView));
                OnPropertyChanged(nameof(IsReviewView));
            }
        }
    }

    public bool IsReviewView
    {
        get => _isReviewView;
        set
        {
            if (!SetProperty(ref _isReviewView, value))
            {
                return;
            }

            if (value)
            {
                _isTodayView = false;
                _isCaptureView = false;
                OnPropertyChanged(nameof(IsTodayView));
                OnPropertyChanged(nameof(IsCaptureView));
            }
        }
    }

    public string Url
    {
        get => _url;
        set
        {
            if (SetProperty(ref _url, value))
            {
                OnPropertyChanged(nameof(CanCapture));
                CaptureCommand.RaiseCanExecuteChanged();
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
                OnPropertyChanged(nameof(CanCapture));
                OnPropertyChanged(nameof(CanAnalyze));
                OnPropertyChanged(nameof(CanSaveActions));
                OnPropertyChanged(nameof(IsEmpty));
                RaiseCommandStates();
            }
        }
    }

    public bool IsAnalyzing
    {
        get => _isAnalyzing;
        private set
        {
            if (SetProperty(ref _isAnalyzing, value))
            {
                OnPropertyChanged(nameof(AnalyzeButtonText));
            }
        }
    }

    public SourcePreview? Preview
    {
        get => _preview;
        private set
        {
            if (SetProperty(ref _preview, value))
            {
                OnPropertyChanged(nameof(HasPreview));
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(CanAnalyze));
                OnPropertyChanged(nameof(PreviewMeta));
                OnPropertyChanged(nameof(PreviewAuthor));
                OnPropertyChanged(nameof(PreviewPublished));
                OnPropertyChanged(nameof(PreviewWordCount));
                RaiseCommandStates();
            }
        }
    }

    public SavedSource? Saved
    {
        get => _saved;
        private set
        {
            if (SetProperty(ref _saved, value))
            {
                OnPropertyChanged(nameof(HasSaved));
                OnPropertyChanged(nameof(IsEmpty));
            }
        }
    }

    public AnalysisResult? Analysis
    {
        get => _analysis;
        private set
        {
            if (ReferenceEquals(_analysis, value))
            {
                return;
            }

            if (_analysis is not null)
            {
                foreach (var action in _analysis.Actions)
                {
                    action.PropertyChanged -= OnActionPropertyChanged;
                }
            }

            if (SetProperty(ref _analysis, value))
            {
                if (_analysis is not null)
                {
                    foreach (var action in _analysis.Actions)
                    {
                        action.PropertyChanged += OnActionPropertyChanged;
                    }
                }

                OnPropertyChanged(nameof(HasAnalysis));
                OnPropertyChanged(nameof(SelectedActionCount));
                OnPropertyChanged(nameof(SelectedActionText));
                OnPropertyChanged(nameof(CanSaveActions));
                OnPropertyChanged(nameof(AnalysisSummary));
                OnPropertyChanged(nameof(NoActionsReason));
                SaveActionsCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public string? AnalysisError
    {
        get => _analysisError;
        private set
        {
            if (SetProperty(ref _analysisError, value))
            {
                OnPropertyChanged(nameof(HasAnalysisError));
            }
        }
    }

    public string? ActionSaveMessage
    {
        get => _actionSaveMessage;
        private set
        {
            if (SetProperty(ref _actionSaveMessage, value))
            {
                OnPropertyChanged(nameof(HasActionSaveMessage));
            }
        }
    }

    public int SourceCount
    {
        get => _sourceCount;
        private set
        {
            if (SetProperty(ref _sourceCount, value))
            {
                OnPropertyChanged(nameof(SourceCountText));
                OnPropertyChanged(nameof(SourceCountLabel));
            }
        }
    }

    public bool HasPreview => Preview is not null;
    public bool HasSaved => Saved is not null;
    public bool HasAnalysis => Analysis is not null;
    public bool IsEmpty => !HasPreview && !HasSaved && !HasAnalysis && !IsBusy;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasAnalysisError => !string.IsNullOrWhiteSpace(AnalysisError);
    public bool HasActionSaveMessage => !string.IsNullOrWhiteSpace(ActionSaveMessage);
    public bool CanCapture => !IsBusy && !string.IsNullOrWhiteSpace(Url);
    public bool CanAnalyze => HasPreview && !IsBusy;
    public bool CanSaveActions => SelectedActionCount > 0 && !IsBusy;
    public bool HasRecent => Recent.Count > 0;
    public bool HasNoRecent => !HasRecent;
    public string RecentEmptyText => HasRecent ? "" : "还没有记录。抓取第一篇文章后，会出现在这里。";
    public string WorkspacePath => _workspace;
    public string SourceCountText => $"{SourceCount} 篇文章 · JSON + Markdown";
    public string SourceCountLabel => $"{SourceCount} 篇文章";
    public string LastCapturedText => Recent.Count == 0 ? "暂无" : Formatting.RelativeDay(Recent[0].FetchedAt);
    public string AnalyzeButtonText => IsAnalyzing ? "AI 分析中…" : "AI 拆解";
    public string AnalysisSummary => Analysis?.Summary ?? "";
    public string? NoActionsReason => Analysis?.NoActionsReason;
    public int SelectedActionCount => Analysis?.Actions.Count(action => action.IsSelected) ?? 0;
    public string SelectedActionText => $"已选 {SelectedActionCount} 条";

    public string PreviewAuthor => Preview?.Author ?? "作者未知";
    public string PreviewPublished => Preview is null
        ? ""
        : Formatting.Date(Preview.PublishedAt ?? Preview.FetchedAt);
    public string PreviewWordCount => Preview is null ? "" : Formatting.WordCount(Preview.WordCount);

    public string PreviewMeta => Preview is null
        ? ""
        : $"{Preview.Author ?? "作者未知"} · {Formatting.Date(Preview.PublishedAt ?? Preview.FetchedAt)} · {Formatting.WordCount(Preview.WordCount)}";

    public AsyncRelayCommand CaptureCommand { get; }
    public AsyncRelayCommand SaveCommand { get; }
    public AsyncRelayCommand AnalyzeCommand { get; }
    public AsyncRelayCommand SaveActionsCommand { get; }
    public AsyncRelayCommand RefreshCommand { get; }
    public RelayCommand DiscardCommand { get; }
    public RelayCommand ClearAnalysisCommand { get; }
    public RelayCommand OpenUrlCommand { get; }
    public RelayCommand OpenWorkspaceCommand { get; }

    public async Task InitializeAsync()
    {
        await RefreshAsync();
        await Actions.InitializeAsync();
    }

    private async Task CaptureAsync()
    {
        var normalized = UrlNormalizer.Normalize(Url);
        if (normalized is null)
        {
            ErrorMessage = "请粘贴一个有效的网址，例如 https://example.com/post。";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;
        AnalysisError = null;
        ActionSaveMessage = null;
        Saved = null;
        Preview = null;
        ClearAnalysis();

        try
        {
            Preview = await _captureService.CaptureAsync(normalized);
            Url = Preview.CanonicalUrl;
        }
        catch (CaptureException error)
        {
            ErrorMessage = error.Message;
        }
        catch (Exception error)
        {
            ErrorMessage = "抓取失败：" + error.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task AnalyzeAsync()
    {
        if (Preview is null)
        {
            return;
        }

        var apiKey = _apiKeyStore.GetKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = await _requestApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return;
            }

            try
            {
                _apiKeyStore.SaveKey(apiKey);
            }
            catch
            {
                // Use the key for this session even if Windows Credential Manager is unavailable.
            }
        }

        IsBusy = true;
        IsAnalyzing = true;
        AnalysisError = null;
        ActionSaveMessage = null;

        try
        {
            Analysis = await _deepSeek.AnalyzeAsync(Preview, apiKey);
            if (Analysis.Actions.Count == 0)
            {
                AnalysisError = Analysis.NoActionsReason ?? "这篇文章没有提取到明确的行动。";
            }
        }
        catch (DeepSeekException error)
        {
            AnalysisError = error.Message;
        }
        catch (Exception error)
        {
            AnalysisError = "AI 拆解失败：" + error.Message;
        }
        finally
        {
            IsAnalyzing = false;
            IsBusy = false;
        }
    }

    private async Task SaveActionsAsync()
    {
        if (Preview is null || Analysis is null)
        {
            return;
        }

        var selected = Analysis.Actions.Where(action => action.IsSelected).ToList();
        if (selected.Count == 0)
        {
            AnalysisError = "至少选一条行动再保存。";
            return;
        }

        IsBusy = true;
        AnalysisError = null;
        ActionSaveMessage = null;

        try
        {
            var paths = await _actionStore.SaveAsync(Preview, selected);
            ActionSaveMessage = $"已保存 {paths.Count} 条行动到 data/actions/";
        }
        catch (Exception error)
        {
            AnalysisError = "保存行动失败：" + error.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SaveAsync()
    {
        if (Preview is null)
        {
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            Saved = await _store.SaveAsync(Preview);
            Preview = null;
            Analysis = null;
            Url = "";
            await RefreshAsync();
        }
        catch (Exception error)
        {
            ErrorMessage = "保存失败：" + error.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RefreshAsync()
    {
        try
        {
            var items = await _store.ListAsync();
            var recent = items.Take(20).ToList();
            for (var index = 0; index < recent.Count; index++)
            {
                recent[index].IsFirst = index == 0;
            }

            Recent.Clear();
            foreach (var item in recent)
            {
                Recent.Add(item);
            }
            SourceCount = items.Count;
            OnPropertyChanged(nameof(LastCapturedText));
        }
        catch (Exception error)
        {
            ErrorMessage = "读取本地数据失败：" + error.Message;
        }
    }

    private void ClearAnalysis()
    {
        Analysis = null;
        AnalysisError = null;
        ActionSaveMessage = null;
    }

    private void Reset()
    {
        Preview = null;
        Saved = null;
        Analysis = null;
        ErrorMessage = null;
        AnalysisError = null;
        ActionSaveMessage = null;
        Url = "";
    }

    private void OnActionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ActionItem.IsSelected))
        {
            return;
        }

        OnPropertyChanged(nameof(SelectedActionCount));
        OnPropertyChanged(nameof(SelectedActionText));
        OnPropertyChanged(nameof(CanSaveActions));
        SaveActionsCommand.RaiseCanExecuteChanged();
    }

    private void RaiseCommandStates()
    {
        CaptureCommand.RaiseCanExecuteChanged();
        SaveCommand.RaiseCanExecuteChanged();
        AnalyzeCommand.RaiseCanExecuteChanged();
        SaveActionsCommand.RaiseCanExecuteChanged();
        RefreshCommand.RaiseCanExecuteChanged();
    }

    private void OpenUrl(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception error)
        {
            ErrorMessage = "打开网址失败：" + error.Message;
        }
    }

    private void OpenWorkspace()
    {
        try
        {
            Directory.CreateDirectory(_workspace);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_workspace}\"")
            {
                UseShellExecute = true,
            });
        }
        catch (Exception error)
        {
            ErrorMessage = "打开文件夹失败：" + error.Message;
        }
    }
}
