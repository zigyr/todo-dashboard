using System.Windows;
using TodoDashboard.Models;
using TodoDashboard.Services;
using TodoDashboard.ViewModels;

namespace TodoDashboard.Views;

public partial class ActionsWindow : Window
{
    private readonly ActionsViewModel _viewModel;

    public ActionsWindow()
    {
        InitializeComponent();

        var workspace = Workspace.Resolve();
        _viewModel = new ActionsViewModel(
            new ActionRepository(workspace),
            PromptDropReasonAsync);
        DataContext = _viewModel;

        Loaded += async (_, _) => await _viewModel.InitializeAsync();
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
