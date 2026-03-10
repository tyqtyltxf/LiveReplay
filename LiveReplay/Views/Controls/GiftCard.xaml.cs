using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LiveReplay.Helpers;
using LiveReplay.Services;

namespace LiveReplay.Views.Controls;

/// <summary>
/// 礼物卡片控件
/// </summary>
public partial class GiftCard : UserControl
{
    // 基准字体大小
    private const double BaseFontSize = 12;
    private const double BasePriceFontSize = 11;
    private const double BasePriceValueFontSize = 13;

    public GiftCard()
    {
        InitializeComponent();
        Loaded += GiftCard_Loaded;
        Unloaded += GiftCard_Unloaded;
    }

    private void GiftCard_Unloaded(object sender, RoutedEventArgs e)
    {
        SettingsService.Instance.SettingsChanged -= OnSettingsChanged;
    }

    private async void GiftCard_Loaded(object sender, RoutedEventArgs e)
    {
        // 订阅设置变更
        SettingsService.Instance.SettingsChanged += OnSettingsChanged;

        if (DataContext is Models.GiftItem gift && !string.IsNullOrEmpty(gift.AvatarUrl))
        {
            var bitmap = await AvatarLoader.LoadAvatarAsync(gift.AvatarUrl);
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
        UserNameText.FontSize = BaseFontSize + offset;
        try
        {
            UserNameText.FontFamily = new FontFamily(settings.UserNameFontFamily);
        }
        catch
        {
            UserNameText.FontFamily = new FontFamily("Microsoft YaHei");
        }

        // 设置礼物名称和数量的字体大小
        var giftInlines = GiftText.Inlines.ToList();
        if (giftInlines.Count >= 3)
        {
            giftInlines[0].FontSize = BaseFontSize + offset;
            giftInlines[1].FontSize = BaseFontSize + offset;
            giftInlines[2].FontSize = BaseFontSize + offset;
        }

        // 设置价格的字体大小
        var priceInlines = PriceText.Inlines.ToList();
        if (priceInlines.Count >= 2)
        {
            priceInlines[0].FontSize = BasePriceFontSize + offset;
            priceInlines[1].FontSize = BasePriceValueFontSize + offset;
        }
    }
}