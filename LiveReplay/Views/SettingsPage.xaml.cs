using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using LiveReplay.Services;

namespace LiveReplay.Views;

/// <summary>
/// 设置页面
/// </summary>
public partial class SettingsPage : Page
{
    private readonly SettingsService _settingsService;
    private bool _isLoaded = false;
    private bool _isLoadingSettings = false;

    public SettingsPage()
    {
        _settingsService = SettingsService.Instance;
        _isLoadingSettings = true; // 防止 InitializeComponent 时触发 TextChanged 覆盖设置
        InitializeComponent();
        LoadSystemFonts();
        Loaded += SettingsPage_Loaded;
    }

    private void LoadSystemFonts()
    {
        // 获取系统所有字体
        var fonts = Fonts.SystemFontFamilies;
        foreach (var font in fonts)
        {
            // 优先使用中文名称，如果没有则使用英文名称
            var fontName = font.FamilyNames.ContainsKey(System.Windows.Markup.XmlLanguage.GetLanguage("zh-cn"))
                ? font.FamilyNames[System.Windows.Markup.XmlLanguage.GetLanguage("zh-cn")]
                : font.Source;

            TimeFontComboBox.Items.Add(fontName);
            UserNameFontComboBox.Items.Add(fontName);
            DanmakuFontComboBox.Items.Add(fontName);
            ScContentFontComboBox.Items.Add(fontName);
        }
    }

    private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_isLoaded)
        {
            _isLoaded = true;
            LoadSettings();
        }
    }

    private void LoadSettings()
    {
        try
        {
            if (_settingsService?.Settings != null)
            {
                _isLoadingSettings = true;
                FontSizeSlider.Value = _settingsService.Settings.BaseFontSize;
                MinPriceTextBox.Text = _settingsService.Settings.MinGiftPrice.ToString("0");

                // 时间字体设置
                TimeColorTextBox.Text = _settingsService.Settings.TimeFontColor;
                TimeFontSizeSlider.Value = _settingsService.Settings.TimeFontSize;
                SelectFontByName(TimeFontComboBox, _settingsService.Settings.TimeFontFamily);

                // 用户名字体设置
                SelectFontByName(UserNameFontComboBox, _settingsService.Settings.UserNameFontFamily);

                // 弹幕字体设置
                SelectFontByName(DanmakuFontComboBox, _settingsService.Settings.DanmakuFontFamily);

                // SC内容字体设置
                SelectFontByName(ScContentFontComboBox, _settingsService.Settings.ScContentFontFamily);

                _isLoadingSettings = false;

                if (!string.IsNullOrEmpty(_settingsService.Settings.BackgroundImagePath))
                {
                    BackgroundPathText.Text = System.IO.Path.GetFileName(_settingsService.Settings.BackgroundImagePath);
                }
            }
        }
        catch (Exception ex)
        {
            _isLoadingSettings = false;
            System.Diagnostics.Debug.WriteLine($"LoadSettings error: {ex.Message}");
        }
    }

    private void SelectFontByName(ComboBox comboBox, string fontName)
    {
        foreach (var item in comboBox.Items)
        {
            if (item.ToString() == fontName)
            {
                comboBox.SelectedItem = item;
                return;
            }
        }
        // 如果找不到，选择默认字体
        if (comboBox.Items.Count > 0)
            comboBox.SelectedIndex = 0;
    }

    private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FontSizeText != null)
        {
            FontSizeText.Text = ((int)e.NewValue).ToString();
        }
    }

    private void SelectBackground_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp|所有文件|*.*",
            Title = "选择背景图片"
        };

        if (dialog.ShowDialog() == true)
        {
            _settingsService.Settings.BackgroundImagePath = dialog.FileName;
            BackgroundPathText.Text = System.IO.Path.GetFileName(dialog.FileName);
        }
    }

    private void ClearBackground_Click(object sender, RoutedEventArgs e)
    {
        _settingsService.Settings.BackgroundImagePath = string.Empty;
        BackgroundPathText.Text = "未选择";
    }

    private void MinPriceTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isLoadingSettings) return;
        if (_settingsService?.Settings != null && double.TryParse(MinPriceTextBox.Text, out var price))
        {
            _settingsService.Settings.MinGiftPrice = price;
        }
    }

    private void TimeFontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings || TimeFontComboBox.SelectedItem == null) return;
        _settingsService.Settings.TimeFontFamily = TimeFontComboBox.SelectedItem.ToString() ?? "Microsoft YaHei";
    }

    private void TimeColorTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isLoadingSettings) return;
        var colorText = TimeColorTextBox.Text;
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(colorText);
            TimeColorPreview.Background = new SolidColorBrush(color);
            _settingsService.Settings.TimeFontColor = colorText;
        }
        catch
        {
            // 颜色格式无效，忽略
        }
    }

    private void TimeFontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TimeFontSizeText != null)
        {
            TimeFontSizeText.Text = ((int)e.NewValue).ToString();
        }
        if (_isLoadingSettings) return;
        _settingsService.Settings.TimeFontSize = (int)e.NewValue;
    }

    private void UserNameFontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings || UserNameFontComboBox.SelectedItem == null) return;
        _settingsService.Settings.UserNameFontFamily = UserNameFontComboBox.SelectedItem.ToString() ?? "Microsoft YaHei";
    }

    private void DanmakuFontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings || DanmakuFontComboBox.SelectedItem == null) return;
        _settingsService.Settings.DanmakuFontFamily = DanmakuFontComboBox.SelectedItem.ToString() ?? "Microsoft YaHei";
    }

    private void ScContentFontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingSettings || ScContentFontComboBox.SelectedItem == null) return;
        _settingsService.Settings.ScContentFontFamily = ScContentFontComboBox.SelectedItem.ToString() ?? "Microsoft YaHei";
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        _settingsService.Settings.BaseFontSize = (int)FontSizeSlider.Value;
        _settingsService.Save();
        MessageBox.Show("设置已保存并应用", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}