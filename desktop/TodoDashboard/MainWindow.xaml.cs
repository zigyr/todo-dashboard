using System.Windows;
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
            PromptForApiKeyAsync);
        DataContext = _viewModel;
    }

    private Task<string?> PromptForApiKeyAsync()
    {
        var dialog = new ApiKeyWindow
        {
            Owner = this,
        };

        return Task.FromResult(dialog.ShowDialog() == true ? dialog.ApiKey : null);
    }
}
