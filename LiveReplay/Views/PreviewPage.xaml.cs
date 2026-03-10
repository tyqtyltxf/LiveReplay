using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveReplay.Models;
using LiveReplay.Services;

namespace LiveReplay.Views;

/// <summary>
/// 预览页面
/// </summary>
public partial class PreviewPage : Page
{
    private readonly SettingsService _settingsService;
    private readonly PreviewPageViewModel _viewModel;

    public PreviewPage()
    {
        InitializeComponent();
        _settingsService = SettingsService.Instance;
        _viewModel = new PreviewPageViewModel();
        DataContext = _viewModel;

        // 应用设置
        ApplySettings();

        // 订阅设置变更事件
        _settingsService.SettingsChanged += OnSettingsChanged;

        Unloaded += PreviewPage_Unloaded;
    }

    private void PreviewPage_Unloaded(object sender, RoutedEventArgs e)
    {
        _settingsService.SettingsChanged -= OnSettingsChanged;
    }

    private void OnSettingsChanged(PlaybackSettings settings)
    {
        ApplySettings();
    }

    private void ApplySettings()
    {
        ApplyBackground();
        ApplyFontSize();
        ApplyGiftFilter();
    }

    private void ApplyBackground()
    {
        var bgPath = _settingsService.Settings.BackgroundImagePath;
        if (!string.IsNullOrEmpty(bgPath) && File.Exists(bgPath))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(bgPath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                BackgroundImage.Source = bitmap;
                VideoBackgroundImage.Source = bitmap;
                MainGrid.Background = Brushes.Transparent;
            }
            catch
            {
                MainGrid.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#262526"));
            }
        }
        else
        {
            BackgroundImage.Source = null;
            VideoBackgroundImage.Source = null;
            MainGrid.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#262526"));
        }
    }

    private void ApplyFontSize()
    {
        var settings = _settingsService.Settings;
        var offset = _settingsService.GetFontSizeOffset();

        // 时间显示设置
        CurrentTimeText.FontSize = settings.TimeFontSize + offset;
        try
        {
            CurrentTimeText.FontFamily = new FontFamily(settings.TimeFontFamily);
        }
        catch
        {
            CurrentTimeText.FontFamily = new FontFamily("Microsoft YaHei");
        }

        try
        {
            CurrentTimeText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(settings.TimeFontColor));
        }
        catch
        {
            CurrentTimeText.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555"));
        }

        _viewModel.FontSizeOffset = offset;
    }

    private void ApplyGiftFilter()
    {
        _viewModel.MinGiftPrice = _settingsService.Settings.MinGiftPrice;
    }
}

/// <summary>
/// 预览页面ViewModel
/// </summary>
public partial class PreviewPageViewModel : ObservableObject
{
    [ObservableProperty]
    private double _fontSizeOffset = 0;

    [ObservableProperty]
    private double _minGiftPrice = 0;

    public ObservableCollection<DanmakuItem> PreviewDanmaku { get; } = new();
    public ObservableCollection<ScItem> PreviewSc { get; } = new();
    public ObservableCollection<GiftItem> AllPreviewGift { get; } = new();

    private ICollectionView? _filteredGifts;
    public ICollectionView FilteredGifts
    {
        get
        {
            if (_filteredGifts == null)
            {
                _filteredGifts = CollectionViewSource.GetDefaultView(AllPreviewGift);
                _filteredGifts.Filter = FilterGift;
            }
            return _filteredGifts;
        }
    }

    private bool FilterGift(object obj)
    {
        if (obj is GiftItem gift)
        {
            return gift.Price >= MinGiftPrice;
        }
        return false;
    }

    partial void OnMinGiftPriceChanged(double value)
    {
        _filteredGifts?.Refresh();
    }

    public PreviewPageViewModel()
    {
        // 添加示例数据
        PreviewDanmaku.Add(new DanmakuItem { UserName = "用户A", Content = "这是一条测试弹幕" });
        PreviewDanmaku.Add(new DanmakuItem { UserName = "用户B", Content = "666666" });
        PreviewDanmaku.Add(new DanmakuItem { UserName = "用户C", Content = "主播好厉害！" });
        PreviewDanmaku.Add(new DanmakuItem { UserName = "用户D", Content = "今天天气真不错，适合直播呢" });

        PreviewSc.Add(new ScItem { UserName = "SC用户A", Content = "这是一条SC测试内容", Price = 30, SendTime = DateTime.Now.AddMinutes(-5) });
        PreviewSc.Add(new ScItem { UserName = "SC用户B", Content = "支持主播！", Price = 100, SendTime = DateTime.Now.AddMinutes(-2) });
        PreviewSc.Add(new ScItem { UserName = "SC用户C", Content = "继续加油！", Price = 500, SendTime = DateTime.Now.AddMinutes(-1) });

        AllPreviewGift.Add(new GiftItem { UserName = "礼物用户A", GiftName = "小电视", GiftCount = 1, Price = 5.2 });
        AllPreviewGift.Add(new GiftItem { UserName = "礼物用户B", GiftName = "舰长", GiftCount = 1, Price = 198, IsGuard = true });
        AllPreviewGift.Add(new GiftItem { UserName = "礼物用户C", GiftName = "礼物", GiftCount = 10, Price = 50 });
    }
}