using CommunityToolkit.Mvvm.ComponentModel;

namespace LiveReplay.Models;

/// <summary>
/// 弹幕数据模型
/// </summary>
public partial class DanmakuItem : ObservableObject
{
    /// <summary>
    /// 时间戳(秒)
    /// </summary>
    [ObservableProperty]
    private double _timestamp;

    /// <summary>
    /// 用户名
    /// </summary>
    [ObservableProperty]
    private string _userName = string.Empty;

    /// <summary>
    /// 用户ID
    /// </summary>
    [ObservableProperty]
    private long _userId;

    /// <summary>
    /// 弹幕内容
    /// </summary>
    [ObservableProperty]
    private string _content = string.Empty;

    /// <summary>
    /// 用户头像URL
    /// </summary>
    [ObservableProperty]
    private string _avatarUrl = string.Empty;

    /// <summary>
    /// 弹幕颜色
    /// </summary>
    [ObservableProperty]
    private int _color = 16777215; // 默认白色
}