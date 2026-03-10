using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LiveReplay.Models;

/// <summary>
/// SC(醒目留言)数据模型
/// </summary>
public partial class ScItem : ObservableObject
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
    /// SC内容
    /// </summary>
    [ObservableProperty]
    private string _content = string.Empty;

    /// <summary>
    /// 价格(元)
    /// </summary>
    [ObservableProperty]
    private int _price;

    /// <summary>
    /// 显示时长(秒)
    /// </summary>
    [ObservableProperty]
    private int _duration;

    /// <summary>
    /// 用户头像URL
    /// </summary>
    [ObservableProperty]
    private string _avatarUrl = string.Empty;

    /// <summary>
    /// 是否已读(双击标记)
    /// </summary>
    [ObservableProperty]
    private bool _isRead;

    /// <summary>
    /// SC发送时间(用于计算相对时间)
    /// </summary>
    [ObservableProperty]
    private DateTime _sendTime;

    /// <summary>
    /// 显示时间(距离SC发出的秒数)
    /// </summary>
    private double _displayTime;
    public double DisplayTime
    {
        get => _displayTime;
        set
        {
            if (SetProperty(ref _displayTime, value))
            {
                OnPropertyChanged(nameof(RelativeTimeText));
            }
        }
    }

    /// <summary>
    /// 获取相对时间文本
    /// </summary>
    public string RelativeTimeText
    {
        get
        {
            if (DisplayTime < 10)
                return "刚刚";
            else if (DisplayTime < 60)
                return $"{(int)DisplayTime}秒前";
            else if (DisplayTime < 3600)
                return $"{(int)(DisplayTime / 60)}分钟前";
            else if (DisplayTime < 86400)
                return $"{(int)(DisplayTime / 3600)}小时前";
            else
                return $"{(int)(DisplayTime / 86400)}天前";
        }
    }

    /// <summary>
    /// 获取价格对应的背景颜色
    /// </summary>
    public string BackgroundColor => Price switch
    {
        >= 2000 => "#AB1A32",
        >= 1000 => "#E54D4D",
        >= 500 => "#E09443",
        >= 100 => "#E2B52B",
        >= 50 => "#427D9E",
        >= 30 => "#2A60B2",
        _ => "#2A60B2"
    };
}