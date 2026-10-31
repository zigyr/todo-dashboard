using System.Diagnostics;
using System.Windows;

namespace TodoDashboard.Views;

public partial class ApiKeyWindow : Window
{
    public ApiKeyWindow()
    {
        InitializeComponent();
    }

    public string ApiKey => KeyBox.Password.Trim();

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ApiKey))
        {
            ErrorText.Text = "请先输入 DeepSeek API Key。";
            KeyBox.Focus();
            return;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Link_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://platform.deepseek.com/api_keys")
            {
                UseShellExecute = true,
            });
        }
        catch (Exception error)
        {
            ErrorText.Text = "打开网页失败：" + error.Message;
        }
    }
}
