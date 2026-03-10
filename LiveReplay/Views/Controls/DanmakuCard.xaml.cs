using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LiveReplay.Helpers;
using LiveReplay.Services;

namespace LiveReplay.Views.Controls;

/// <summary>
/// 弹幕卡片控件
/// </summary>
public partial class DanmakuCard : UserControl
{
    private const double MaxContentWidth = 500;
    // 左侧margin + 右侧margin + padding + 装饰条
    private const double ExtraWidth = 50 + 10 + 10 + 14 + 6 + 8;

    // 基准字体大小
    private const double BaseUserNameFontSize = 10;
    private const double BaseContentFontSize = 13;

    public DanmakuCard()
    {
        InitializeComponent();
        Loaded += DanmakuCard_Loaded;
        Unloaded += DanmakuCard_Unloaded;
    }

    private void DanmakuCard_Unloaded(object sender, RoutedEventArgs e)
    {
        SettingsService.Instance.SettingsChanged -= OnSettingsChanged;
    }

    private async void DanmakuCard_Loaded(object sender, RoutedEventArgs e)
    {
        // 订阅设置变更
        SettingsService.Instance.SettingsChanged += OnSettingsChanged;

        if (DataContext is Models.DanmakuItem danmaku)
        {
            // 加载头像
            if (!string.IsNullOrEmpty(danmaku.AvatarUrl))
            {
                var bitmap = await AvatarLoader.LoadAvatarAsync(danmaku.AvatarUrl);
                if (bitmap != null)
                {
                    AvatarImage.Source = bitmap;
                }
            }

            // 设置弹幕内容颜色
            if (danmaku.Color != 16777215) // 非默认白色
            {
                var color = ColorFromInt(danmaku.Color);
                ContentText.Foreground = new SolidColorBrush(color);
            }
        }

        // 计算弹幕内容最大宽度
        UpdateContentMaxWidth();

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

        // 应用弹幕内容字体
        ContentText.FontSize = BaseContentFontSize + offset;
        try
        {
            ContentText.FontFamily = new FontFamily(settings.DanmakuFontFamily);
        }
        catch
        {
            ContentText.FontFamily = new FontFamily("Microsoft YaHei");
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        UpdateContentMaxWidth();
    }

    private void UpdateContentMaxWidth()
    {
        // 获取父容器的实际宽度（弹幕区域宽度）
        var parent = Parent as FrameworkElement;
        if (parent == null) return;

        var availableWidth = parent.ActualWidth;
        if (availableWidth <= 0) return;

        // 计算内容可用的最大宽度：区域宽度 - 额外空间（margin/padding等）
        var contentMaxWidth = availableWidth - ExtraWidth;

        // 取可用宽度和500的较小值，且最小为100
        if (contentMaxWidth > MaxContentWidth)
        {
            contentMaxWidth = MaxContentWidth;
        }
        else if (contentMaxWidth < 100)
        {
            contentMaxWidth = 100;
        }

        ContentText.MaxWidth = contentMaxWidth;
    }

    /// <summary>
    /// 将整数颜色值转换为Color
    /// </summary>
    private static Color ColorFromInt(int colorValue)
    {
        // B站颜色格式: RRGGBB (十进制)
        var r = (byte)((colorValue >> 16) & 0xFF);
        var g = (byte)((colorValue >> 8) & 0xFF);
        var b = (byte)(colorValue & 0xFF);
        return Color.FromRgb(r, g, b);
    }
}