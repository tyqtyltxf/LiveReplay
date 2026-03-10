using System.Windows.Controls;

namespace LiveReplay.Views;

/// <summary>
/// 占位页面
/// </summary>
public partial class PlaceholderPage : Page
{
    public PlaceholderPage(string message = "功能开发中...")
    {
        InitializeComponent();
        MessageText.Text = message;
    }
}