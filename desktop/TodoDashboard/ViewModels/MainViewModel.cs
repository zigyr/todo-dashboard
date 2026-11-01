using System.Diagnostics;
using System.IO;
using TodoDashboard.Models;
using TodoDashboard.Services;

namespace TodoDashboard.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly UrlCaptureService _captureService;
    private readonly DeepSeekService _deepSeek;
    private readonly ApiKeyStore _apiKeyStore;
    private readonly Func<Task<string?>> _requestApiKey;

    private string _url = "";
    private bool _isBusy;
    private bool _isSummaryView = true;
    private bool _isHistoryView;
    private SourcePreview? _preview;
    private ArticleAnalysis? _analysis;
    private string? _errorMessage;

    public MainViewModel(
        UrlCaptureService captureService,
        DeepSeekService deepSeek,
        ApiKeyStore apiKeyStore,
        HistoryStore historyStore,
        Func<Task<string?>> requestApiKey,
        Func<Task<string?>> pickCsv)
    {
        _captureService = captureService;
        _deepSeek = deepSeek;
        _apiKeyStore = apiKeyStore;
        _requestApiKey = requestApiKey;
        History = new HistoryViewModel(historyStore, pickCsv);

        SummarizeCommand = new AsyncRelayCommand(SummarizeAsync, () => CanSummarize);
        OpenUrlCommand = new RelayCommand(_ => OpenUrl());
        ResetCommand = new RelayCommand(_ => Reset());
    }

    public HistoryViewModel History { get; }

    public bool IsSummaryView
    {
        get => _isSummaryView;
        set
        {
            if (SetProperty(ref _isSummaryView, value) && value)
            {
                _isHistoryView = false;
                OnPropertyChanged(nameof(IsHistoryView));
            }
        }
    }

    public bool IsHistoryView
    {
        get => _isHistoryView;
        set
        {
            if (SetProperty(ref _isHistoryView, value) && value)
            {
                _isSummaryView = false;
                OnPropertyChanged(nameof(IsSummaryView));
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
                OnPropertyChanged(nameof(CanSummarize));
                SummarizeCommand.RaiseCanExecuteChanged();
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
                OnPropertyChanged(nameof(CanSummarize));
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(ButtonText));
                SummarizeCommand.RaiseCanExecuteChanged();
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
                OnPropertyChanged(nameof(ResultTitle));
                OnPropertyChanged(nameof(ResultMeta));
            }
        }
    }

    public ArticleAnalysis? Analysis
    {
        get => _analysis;
        private set
        {
            if (SetProperty(ref _analysis, value))
            {
                OnPropertyChanged(nameof(HasResult));
                OnPropertyChanged(nameof(IsEmpty));
                OnPropertyChanged(nameof(Summary));
                OnPropertyChanged(nameof(Explanation));
                OnPropertyChanged(nameof(WhoItSuits));
                OnPropertyChanged(nameof(ReadingTip));
                OnPropertyChanged(nameof(HasReadingTip));
                OnPropertyChanged(nameof(ResultTitle));
                OnPropertyChanged(nameof(ResultMeta));
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

    public bool CanSummarize => !IsBusy && !string.IsNullOrWhiteSpace(Url);
    public bool HasResult => Analysis is not null;
    public bool IsEmpty => !HasResult && !IsBusy;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasReadingTip => !string.IsNullOrWhiteSpace(ReadingTip);
    public string ButtonText => IsBusy ? "AI 正在读…" : "生成摘要";

    public string ResultTitle => Preview?.Title ?? "";
    public string ResultMeta => Preview is null
        ? ""
        : $"{Preview.Author ?? "作者未知"} · {Formatting.Date(Preview.PublishedAt ?? Preview.FetchedAt)} · {Formatting.WordCount(Preview.WordCount)}";

    public string Summary => Analysis?.Summary ?? "";
    public string Explanation => Analysis?.Explanation ?? "";
    public string WhoItSuits => Analysis?.WhoItSuits ?? "";
    public string ReadingTip => Analysis?.ReadingTip ?? "";

    public AsyncRelayCommand SummarizeCommand { get; }
    public RelayCommand OpenUrlCommand { get; }
    public RelayCommand ResetCommand { get; }

    public Task InitializeAsync() => History.InitializeAsync();

    private async Task SummarizeAsync()
    {
        var normalized = UrlNormalizer.Normalize(Url);
        if (normalized is null)
        {
            ErrorMessage = "请粘贴一个有效的网址，例如 https://example.com/post。";
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
        ErrorMessage = null;
        Analysis = null;
        Preview = null;

        try
        {
            Preview = await _captureService.CaptureAsync(normalized);
            Analysis = await _deepSeek.AnalyzeAsync(Preview, apiKey);

            var entry = new HistoryEntry
            {
                CreatedAt = DateTimeOffset.Now,
                Url = Preview.CanonicalUrl,
                Title = Preview.Title,
                Author = Preview.Author ?? "",
                PublishedAt = Formatting.Date(Preview.PublishedAt ?? Preview.FetchedAt),
                WordCount = Preview.WordCount,
                Summary = Analysis.Summary,
            };

            try
            {
                await History.AddAsync(entry);
            }
            catch (Exception historyError)
            {
                ErrorMessage = "摘要已生成，但历史保存失败：" + historyError.Message;
            }
        }
        catch (CaptureException error)
        {
            ErrorMessage = error.Message;
        }
        catch (DeepSeekException error)
        {
            ErrorMessage = error.Message;
        }
        catch (Exception error)
        {
            ErrorMessage = "生成摘要失败：" + error.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OpenUrl()
    {
        var target = Preview?.CanonicalUrl;
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

    private void Reset()
    {
        Preview = null;
        Analysis = null;
        ErrorMessage = null;
        Url = "";
    }
}
