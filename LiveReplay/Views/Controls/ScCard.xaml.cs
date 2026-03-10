using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LiveReplay.Helpers;
using LiveReplay.Services;

namespace LiveReplay.Views.Controls;

/// <summary>
/// SC卡片控件
/// </summary>
public partial class ScCard : UserControl
{
    // 基准字体大小
    private const double BaseUserNameFontSize = 13;
    private const double BasePriceFontSize = 12;
    private const double BaseTimeFontSize = 11;
    private const double BaseContentFontSize = 13;

    public ScCard()
    {
        InitializeComponent();
        Loaded += ScCard_Loaded;
        Unloaded += ScCard_Unloaded;
    }

    private void ScCard_Unloaded(object sender, RoutedEventArgs e)
    {
        SettingsService.Instance.SettingsChanged -= OnSettingsChanged;
    }

    private async void ScCard_Loaded(object sender, RoutedEventArgs e)
    {
        // 订阅设置变更
        SettingsService.Instance.SettingsChanged += OnSettingsChanged;

        if (DataContext is Models.ScItem sc && !string.IsNullOrEmpty(sc.AvatarUrl))
        {
            var bitmap = await AvatarLoader.LoadAvatarAsync(sc.AvatarUrl);
            if (bitmap != null)
            {
                AvatarImage.Source = bitmap;
            }
        }

        // 应用字体设置
        ApplyFontSettings();
    }

    private void OnSettingsChanged(Models.PlaybackSettings settings)
    {
        ApplyFontSettings();
    }

    private void ApplyFontSettings()
    {
        var settings = SettingsService.Instance.Settings;
        var offset = SettingsService.Instance.GetFontSizeOffset();

        // 应用用户名字体
        UserNameText.FontSize = BaseUserNameFontSize + offset;
        try
        {
            UserNameText.FontFamily = new FontFamily(settings.UserNameFontFamily);
        }
        catch
        {
            UserNameText.FontFamily = new FontFamily("Microsoft YaHei");
        }

        PriceText.FontSize = BasePriceFontSize + offset;
        TimeText.FontSize = BaseTimeFontSize + offset;

        // 应用SC内容字体
        ContentText.FontSize = BaseContentFontSize + offset;
        try
        {
            ContentText.FontFamily = new FontFamily(settings.ScContentFontFamily);
        }
        catch
        {
            ContentText.FontFamily = new FontFamily("Microsoft YaHei");
        }
    }

    private void ScCard_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) // 双击
        {
            if (DataContext is Models.ScItem sc)
            {
                sc.IsRead = !sc.IsRead;
                CardBorder.Opacity = sc.IsRead ? 0.4 : 1.0;
            }
        }
    }
}