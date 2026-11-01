using System.Windows;
using Microsoft.Win32;
using TodoDashboard.Services;
using TodoDashboard.ViewModels;
using TodoDashboard.Views;

namespace TodoDashboard;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel(
            new UrlCaptureService(),
            new DeepSeekService(),
            new ApiKeyStore(),
            HistoryStore.Resolve(),
            PromptForApiKeyAsync,
            PromptForCsvAsync);
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.InitializeAsync();
    }

    private Task<string?> PromptForApiKeyAsync()
    {
        var dialog = new ApiKeyWindow
        {
            Owner = this,
        };

        return Task.FromResult(dialog.ShowDialog() == true ? dialog.ApiKey : null);
    }

    private Task<string?> PromptForCsvAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "导入历史 CSV（会覆盖本地 CSV）",
            Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
            CheckFileExists = true,
        };

        return Task.FromResult(dialog.ShowDialog() == true ? dialog.FileName : null);
    }
}
