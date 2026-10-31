using System.Windows;
using TodoDashboard.Models;
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

        var workspace = Workspace.Resolve();
        _viewModel = new MainViewModel(
            new UrlCaptureService(),
            new SourceStore(workspace),
            new ActionStore(workspace),
            new DeepSeekService(),
            new ApiKeyStore(),
            workspace,
            PromptForApiKeyAsync,
            PromptDropReasonAsync);
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

    private Task<string?> PromptDropReasonAsync(ActionRecord action)
    {
        var dialog = new DropReasonWindow(action.Title)
        {
            Owner = this,
        };

        return Task.FromResult(dialog.ShowDialog() == true ? dialog.Reason : null);
    }
}
