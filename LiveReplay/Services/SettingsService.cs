using System;
using System.IO;
using System.Text.Json;
using LiveReplay.Models;

namespace LiveReplay.Services;

/// <summary>
/// 设置服务 - 全局单例，负责设置的保存和加载
/// </summary>
public class SettingsService
{
    private static SettingsService? _instance;
    private static readonly object _lock = new();

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "LiveReplay",
        "settings.json"
    );

    public PlaybackSettings Settings { get; private set; } = new();

    /// <summary>
    /// 设置变更事件
    /// </summary>
    public event Action<PlaybackSettings>? SettingsChanged;

    private SettingsService()
    {
        var loaded = Load();
        if (loaded != null)
        {
            Settings = loaded;
        }
    }

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static SettingsService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new SettingsService();
                }
            }
            return _instance;
        }
    }

    private PlaybackSettings Load()
    {
        if (!File.Exists(SettingsPath))
        {
            return new PlaybackSettings();
        }

        try
        {
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<PlaybackSettings>(json) ?? new PlaybackSettings();
        }
        catch
        {
            return new PlaybackSettings();
        }
    }

    public void Save()
    {
        var directory = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(SettingsPath, json);

        // 触发设置变更事件
        SettingsChanged?.Invoke(Settings);
    }

    /// <summary>
    /// 获取相对于基准字号(14)的字号偏移量
    /// </summary>
    public double GetFontSizeOffset()
    {
        return Settings.BaseFontSize - 14;
    }

    /// <summary>
    /// 根据基准字号计算实际字号
    /// </summary>
    public double GetActualFontSize(double baseSize)
    {
        return baseSize + GetFontSizeOffset();
    }
}