using CommunityToolkit.Mvvm.ComponentModel;

namespace LiveReplay.Models;

/// <summary>
/// 礼物数据模型
/// </summary>
public partial class GiftItem : ObservableObject
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
    /// 礼物名称
    /// </summary>
    [ObservableProperty]
    private string _giftName = string.Empty;

    /// <summary>
    /// 礼物数量
    /// </summary>
    [ObservableProperty]
    private int _giftCount;

    /// <summary>
    /// 价格(元) - 原始price/1000
    /// </summary>
    [ObservableProperty]
    private double _price;

    /// <summary>
    /// 用户头像URL
    /// </summary>
    [ObservableProperty]
    private string _avatarUrl = string.Empty;

    /// <summary>
    /// 是否为舰长礼物
    /// </summary>
    [ObservableProperty]
    private bool _isGuard;

    /// <summary>
    /// 舰长等级(1=总督, 2=提督, 3=舰长)
    /// </summary>
    [ObservableProperty]
    private int _guardLevel;
}