using System.Windows;
using System.Windows.Controls;

namespace TodoDashboard.Views;

public partial class DropReasonWindow : Window
{
    public DropReasonWindow(string actionTitle)
    {
        InitializeComponent();
        ActionTitleText.Text = actionTitle;
        ReasonBox.Focus();
    }

    public string Reason => ReasonBox.Text.Trim();

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string preset)
        {
            ReasonBox.Text = preset;
            ReasonBox.Focus();
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Reason))
        {
            ErrorText.Text = "写一句原因再放弃。";
            ReasonBox.Focus();
            return;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
