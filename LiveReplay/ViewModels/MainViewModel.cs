using System;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveReplay.Views;

namespace LiveReplay.ViewModels;

/// <summary>
/// 主窗口ViewModel - 负责页面导航
/// </summary>
public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private int _selectedNavIndex = 0;

    // 当前播放窗口引用
    private PlaybackWindow? _currentPlaybackWindow;

    /// <summary>
    /// 导航请求事件
    /// </summary>
    public event Action<Page>? NavigateRequested;

    public MainViewModel()
    {
    }

    [RelayCommand]
    public void NavigateToReplay()
    {
        SelectedNavIndex = 0;
        NavigateRequested?.Invoke(new ReplayPage());
    }

    [RelayCommand]
    private void NavigateToLive()
    {
        SelectedNavIndex = 1;
        NavigateRequested?.Invoke(new PlaceholderPage("直播功能开发中..."));
    }

    [RelayCommand]
    private void NavigateToPreview()
    {
        SelectedNavIndex = 2;
        NavigateRequested?.Invoke(new PreviewPage());
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        SelectedNavIndex = 3;
        NavigateRequested?.Invoke(new SettingsPage());
    }

    /// <summary>
    /// 打开播放窗口
    /// </summary>
    public void OpenPlaybackWindow(string videoPath, string xmlPath)
    {
        // 如果已有播放窗口，先关闭
        if (_currentPlaybackWindow != null)
        {
            // 移除事件处理并关闭窗口
            // 不显式调用Cleanup，让窗口自然关闭时自行清理
            _currentPlaybackWindow.Closed -= OnPlaybackWindowClosed;
            try
            {
                _currentPlaybackWindow.Close();
            }
            catch { }
            _currentPlaybackWindow = null;
        }

        // 创建新的播放窗口
        _currentPlaybackWindow = new PlaybackWindow(videoPath, xmlPath);
        _currentPlaybackWindow.Closed += OnPlaybackWindowClosed;
        _currentPlaybackWindow.Show();
    }

    private void OnPlaybackWindowClosed(object? sender, EventArgs e)
    {
        if (_currentPlaybackWindow != null)
        {
            _currentPlaybackWindow.Closed -= OnPlaybackWindowClosed;
            _currentPlaybackWindow = null;
        }
    }
}