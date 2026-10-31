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
    private readonly string _workspace;

    private string _url = "";
    private bool _isBusy;
    private SourcePreview? _preview;
    private SavedSource? _saved;
    private string? _errorMessage;
    private int _sourceCount;

    public MainViewModel(UrlCaptureService captureService, SourceStore store, string workspace)
    {
        _captureService = captureService;
        _store = store;
        _workspace = workspace;

        CaptureCommand = new AsyncRelayCommand(CaptureAsync, () => CanCapture);
        SaveCommand = new AsyncRelayCommand(SaveAsync, () => HasPreview && !IsBusy);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync, () => !IsBusy);
        DiscardCommand = new RelayCommand(_ => Reset());
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
                OnPropertyChanged(nameof(IsEmpty));
                CaptureCommand.RaiseCanExecuteChanged();
                SaveCommand.RaiseCanExecuteChanged();
                RefreshCommand.RaiseCanExecuteChanged();
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
                OnPropertyChanged(nameof(PreviewMeta));
                OnPropertyChanged(nameof(PreviewAuthor));
                OnPropertyChanged(nameof(PreviewPublished));
                OnPropertyChanged(nameof(PreviewWordCount));
                SaveCommand.RaiseCanExecuteChanged();
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
    public bool IsEmpty => !HasPreview && !HasSaved && !IsBusy;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool CanCapture => !IsBusy && !string.IsNullOrWhiteSpace(Url);
    public bool HasRecent => Recent.Count > 0;
    public bool HasNoRecent => !HasRecent;
    public string RecentEmptyText => HasRecent ? "" : "还没有记录。抓取第一篇文章后，会出现在这里。";
    public string WorkspacePath => _workspace;
    public string SourceCountText => $"{SourceCount} 篇文章 · JSON + Markdown";
    public string SourceCountLabel => $"{SourceCount} 篇文章";
    public string LastCapturedText => Recent.Count == 0 ? "暂无" : Formatting.RelativeDay(Recent[0].FetchedAt);

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
    public AsyncRelayCommand RefreshCommand { get; }
    public RelayCommand DiscardCommand { get; }
    public RelayCommand OpenUrlCommand { get; }
    public RelayCommand OpenWorkspaceCommand { get; }

    public Task InitializeAsync() => RefreshAsync();

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
        Saved = null;
        Preview = null;

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

    private void Reset()
    {
        Preview = null;
        Saved = null;
        ErrorMessage = null;
        Url = "";
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
