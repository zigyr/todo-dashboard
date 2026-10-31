using System.Windows;
using TodoDashboard.Services;
using TodoDashboard.ViewModels;

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
            workspace);
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.InitializeAsync();
    }
}
