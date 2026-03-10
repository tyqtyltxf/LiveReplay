using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using LiveReplay.Models;
using LiveReplay.Services;

namespace LiveReplay.Views;

/// <summary>
/// 独立播放窗口
/// </summary>
public partial class PlaybackWindow : Window
{
    private readonly PlaybackWindowViewModel _viewModel;
    private readonly DanmakuParser _parser = new();
    private readonly SettingsService _settingsService;
    private DispatcherTimer? _updateTimer;
    private bool _isDragging = false;
    private bool _isPlaying = false;

    private LibVLC? _libVLC;
    private LibVLCSharp.Shared.MediaPlayer? _mediaPlayer;
    private VideoView? _videoView;

    public PlaybackWindow(string videoPath, string xmlPath)
    {
        InitializeComponent();

        _settingsService = SettingsService.Instance;

        _viewModel = new PlaybackWindowViewModel
        {
            VideoPath = videoPath,
            XmlPath = xmlPath,
            MinGiftPrice = _settingsService.Settings.MinGiftPrice
        };
        DataContext = _viewModel;

        TitleText.Text = Path.GetFileNameWithoutExtension(videoPath);
        Title = "LiveReplay - " + Path.GetFileNameWithoutExtension(videoPath);

        // 应用设置
        ApplySettings();

        // 订阅设置变更事件
        _settingsService.SettingsChanged += OnSettingsChanged;

        Loaded += PlaybackWindow_Loaded;
        Closed += PlaybackWindow_Closed;
    }

    private void ApplySettings()
    {
        // 应用背景
        ApplyBackground();

        // 应用字号
        ApplyFontSize();
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

        // 更新ViewModel中的字号偏移，供卡片使用
        _viewModel.FontSizeOffset = offset;
    }

    private void OnSettingsChanged(PlaybackSettings settings)
    {
        Dispatcher.Invoke(() =>
        {
            _viewModel.MinGiftPrice = settings.MinGiftPrice;
            ApplySettings();
        });
    }

    private void PlaybackWindow_Closed(object? sender, EventArgs e)
    {
        _settingsService.SettingsChanged -= OnSettingsChanged;
        Cleanup();
    }

    private void PlaybackWindow_Loaded(object sender, RoutedEventArgs e)
    {
        LoadDanmakuData();
        InitializeVLC();
    }

    private void InitializeVLC()
    {
        try
        {
            Core.Initialize();

            _libVLC = new LibVLC();
            _mediaPlayer = new LibVLCSharp.Shared.MediaPlayer(_libVLC);

            _videoView = new VideoView
            {
                MediaPlayer = _mediaPlayer
            };
            VideoControl.Content = _videoView;

            var media = new Media(_libVLC, new Uri(_viewModel.VideoPath));
            _mediaPlayer.Play(media);

            _isPlaying = true;
            PlayPauseButton.Content = "\uE769";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开视频文件失败: {ex.Message}\n\n请确保已安装VLC媒体播放器。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        _updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        _updateTimer.Tick += UpdateTimer_Tick;
        _updateTimer.Start();
    }

    private async void LoadDanmakuData()
    {
        try
        {
            var (danmakuList, scList, giftList, startTime) = await _parser.ParseAsync(_viewModel.XmlPath);

            _viewModel.StartTime = startTime;
            _viewModel.DanmakuList = new ObservableCollection<DanmakuItem>(danmakuList);
            _viewModel.ScList = new ObservableCollection<ScItem>(scList);
            _viewModel.GiftList = new ObservableCollection<GiftItem>(giftList);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"解析弹幕文件失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateTimer_Tick(object? sender, EventArgs e)
    {
        if (_isDragging || _mediaPlayer == null) return;

        try
        {
            var position = _mediaPlayer.Time / 1000.0;
            var duration = _mediaPlayer.Length / 1000.0;

            if (duration > 0)
            {
                ProgressSlider.Maximum = duration;
                ProgressSlider.Value = position;
            }

            var currentTime = _viewModel.StartTime.AddSeconds(position);
            CurrentTimeText.Text = currentTime.ToString("HH:mm:ss");

            _viewModel.CurrentPlaybackTime = position;
            UpdateScDisplayTimes(position);

            SyncDanmaku(position);
            SyncSc(position);
            SyncGift(position);
        }
        catch { }
    }

    private void UpdateScDisplayTimes(double currentPosition)
    {
        foreach (var sc in _viewModel.VisibleSc)
        {
            var newDisplayTime = currentPosition - sc.Timestamp;
            if (newDisplayTime < 0) newDisplayTime = 0;
            sc.DisplayTime = newDisplayTime;
        }
    }

    private int _lastDanmakuIndex = -1;
    private void SyncDanmaku(double position)
    {
        int i = _lastDanmakuIndex + 1;
        while (i < _viewModel.DanmakuList.Count)
        {
            if (_viewModel.DanmakuList[i].Timestamp <= position)
            {
                var danmaku = _viewModel.DanmakuList[i];
                Dispatcher.Invoke(() =>
                {
                    _viewModel.VisibleDanmaku.Insert(0, danmaku);
                    while (_viewModel.VisibleDanmaku.Count > 50)
                    {
                        _viewModel.VisibleDanmaku.RemoveAt(_viewModel.VisibleDanmaku.Count - 1);
                    }
                });
                _lastDanmakuIndex = i;
                i++;
            }
            else break;
        }
    }

    private int _lastScIndex = -1;
    private void SyncSc(double position)
    {
        for (int i = _lastScIndex + 1; i < _viewModel.ScList.Count; i++)
        {
            if (_viewModel.ScList[i].Timestamp <= position)
            {
                var sc = _viewModel.ScList[i];
                sc.DisplayTime = position - sc.Timestamp;
                Dispatcher.Invoke(() =>
                {
                    _viewModel.VisibleSc.Insert(0, sc);
                });
                _lastScIndex = i;
            }
            else break;
        }
    }

    private int _lastGiftIndex = -1;
    private void SyncGift(double position)
    {
        for (int i = _lastGiftIndex + 1; i < _viewModel.GiftList.Count; i++)
        {
            if (_viewModel.GiftList[i].Timestamp <= position)
            {
                var gift = _viewModel.GiftList[i];
                if (gift.Price >= _viewModel.MinGiftPrice)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _viewModel.VisibleGift.Insert(0, gift);
                        while (_viewModel.VisibleGift.Count > 30)
                        {
                            _viewModel.VisibleGift.RemoveAt(_viewModel.VisibleGift.Count - 1);
                        }
                    });
                }
                _lastGiftIndex = i;
            }
            else break;
        }
    }

    public void Cleanup()
    {
        _updateTimer?.Stop();
        _updateTimer = null;

        try
        {
            _mediaPlayer?.Dispose();
            _mediaPlayer = null;
        }
        catch { }

        try
        {
            _libVLC?.Dispose();
            _libVLC = null;
        }
        catch { }

        try
        {
            if (_videoView != null)
            {
                _videoView.MediaPlayer = null;
                _videoView = null;
            }
        }
        catch { }
    }

    private void PlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (_mediaPlayer == null) return;

        if (_isPlaying)
        {
            _mediaPlayer.Pause();
            _isPlaying = false;
            PlayPauseButton.Content = "\uE768";
        }
        else
        {
            _mediaPlayer.Play();
            _isPlaying = true;
            PlayPauseButton.Content = "\uE769";
        }
    }

    private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isDragging)
        {
            CurrentTimeText.Text = _viewModel.StartTime.AddSeconds(e.NewValue).ToString("HH:mm:ss");
        }
    }

    private void ProgressSlider_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _isDragging = true;
    }

    private void ProgressSlider_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _isDragging = false;
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Time = (long)(ProgressSlider.Value * 1000);
        }
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Volume = (int)e.NewValue;
        }
    }
}

/// <summary>
/// 播放窗口ViewModel
/// </summary>
public partial class PlaybackWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private string _videoPath = string.Empty;

    [ObservableProperty]
    private string _xmlPath = string.Empty;

    [ObservableProperty]
    private DateTime _startTime = DateTime.Now;

    [ObservableProperty]
    private double _minGiftPrice = 0;

    [ObservableProperty]
    private double _currentPlaybackTime = 0;

    [ObservableProperty]
    private double _fontSizeOffset = 0;

    public ObservableCollection<DanmakuItem> DanmakuList { get; set; } = new();
    public ObservableCollection<ScItem> ScList { get; set; } = new();
    public ObservableCollection<GiftItem> GiftList { get; set; } = new();

    public ObservableCollection<DanmakuItem> VisibleDanmaku { get; } = new();
    public ObservableCollection<ScItem> VisibleSc { get; } = new();
    public ObservableCollection<GiftItem> VisibleGift { get; } = new();
}