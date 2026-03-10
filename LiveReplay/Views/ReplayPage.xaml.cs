using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveReplay.Services;

namespace LiveReplay.Views;

/// <summary>
/// 回放选择页面
/// </summary>
public partial class ReplayPage : Page
{
    private readonly SettingsService _settingsService;

    public ReplayPage()
    {
        InitializeComponent();
        _settingsService = SettingsService.Instance;
        DataContext = new ReplayPageViewModel();
        Loaded += ReplayPage_Loaded;
    }

    private void ReplayPage_Loaded(object sender, RoutedEventArgs e)
    {
        var vm = (ReplayPageViewModel)DataContext;
        // 从设置中恢复文件路径
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastVideoPath) && File.Exists(_settingsService.Settings.LastVideoPath))
        {
            vm.VideoPath = _settingsService.Settings.LastVideoPath;
        }
        if (!string.IsNullOrEmpty(_settingsService.Settings.LastXmlPath) && File.Exists(_settingsService.Settings.LastXmlPath))
        {
            vm.XmlPath = _settingsService.Settings.LastXmlPath;
        }
    }

    private void SelectVideo_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "视频文件|*.mp4;*.flv|所有文件|*.*",
            Title = "选择视频文件"
        };

        if (dialog.ShowDialog() == true)
        {
            ((ReplayPageViewModel)DataContext).VideoPath = dialog.FileName;
        }
    }

    private void SelectXml_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "XML文件|*.xml|所有文件|*.*",
            Title = "选择弹幕文件"
        };

        if (dialog.ShowDialog() == true)
        {
            ((ReplayPageViewModel)DataContext).XmlPath = dialog.FileName;
        }
    }

    private void StartPlayback_Click(object sender, RoutedEventArgs e)
    {
        var vm = (ReplayPageViewModel)DataContext;

        if (vm.CanStart)
        {
            // 保存文件路径到设置
            _settingsService.Settings.LastVideoPath = vm.VideoPath;
            _settingsService.Settings.LastXmlPath = vm.XmlPath;
            _settingsService.Save();

            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                var mainVm = mainWindow.DataContext as ViewModels.MainViewModel;
                mainVm?.OpenPlaybackWindow(vm.VideoPath, vm.XmlPath);
            }
        }
    }
}

/// <summary>
/// 回放页面ViewModel
/// </summary>
public partial class ReplayPageViewModel : ObservableObject
{
    private string _videoPath = string.Empty;
    private string _xmlPath = string.Empty;

    public string VideoPath
    {
        get => _videoPath;
        set
        {
            if (SetProperty(ref _videoPath, value))
            {
                OnPropertyChanged(nameof(CanStart));
            }
        }
    }

    public string XmlPath
    {
        get => _xmlPath;
        set
        {
            if (SetProperty(ref _xmlPath, value))
            {
                OnPropertyChanged(nameof(CanStart));
            }
        }
    }

    public bool CanStart => !string.IsNullOrEmpty(VideoPath) && !string.IsNullOrEmpty(XmlPath)
        && File.Exists(VideoPath) && File.Exists(XmlPath);
}