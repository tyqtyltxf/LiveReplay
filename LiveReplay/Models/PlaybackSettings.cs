using CommunityToolkit.Mvvm.ComponentModel;

namespace LiveReplay.Models;

/// <summary>
/// 播放设置
/// </summary>
public partial class PlaybackSettings : ObservableObject
{
    /// <summary>
    /// 基准字号
    /// </summary>
    [ObservableProperty]
    private int _baseFontSize = 14;

    /// <summary>
    /// 背景图片路径
    /// </summary>
    [ObservableProperty]
    private string _backgroundImagePath = string.Empty;

    /// <summary>
    /// 礼物最低显示价格(元)
    /// </summary>
    [ObservableProperty]
    private double _minGiftPrice = 0;

    /// <summary>
    /// 弹幕区与礼物区分隔比例(弹幕区占比)
    /// </summary>
    [ObservableProperty]
    private double _danmakuGiftRatio = 0.8; // 默认4:1

    /// <summary>
    /// 三列宽度比例
    /// </summary>
    [ObservableProperty]
    private string _columnWidths = "1,2,1"; // 默认等比

    /// <summary>
    /// 最后使用的视频文件路径
    /// </summary>
    [ObservableProperty]
    private string _lastVideoPath = string.Empty;

    /// <summary>
    /// 最后使用的XML弹幕文件路径
    /// </summary>
    [ObservableProperty]
    private string _lastXmlPath = string.Empty;

    #region 时间字体设置
    /// <summary>
    /// 时间字体名称
    /// </summary>
    [ObservableProperty]
    private string _timeFontFamily = "Microsoft YaHei";

    /// <summary>
    /// 时间字体颜色 (十六进制)
    /// </summary>
    [ObservableProperty]
    private string _timeFontColor = "#555555";

    /// <summary>
    /// 时间字体大小
    /// </summary>
    [ObservableProperty]
    private int _timeFontSize = 18;
    #endregion

    #region 用户名字体设置
    /// <summary>
    /// 用户名字体名称
    /// </summary>
    [ObservableProperty]
    private string _userNameFontFamily = "Microsoft YaHei";
    #endregion

    #region 弹幕字体设置
    /// <summary>
    /// 弹幕内容字体名称
    /// </summary>
    [ObservableProperty]
    private string _danmakuFontFamily = "Microsoft YaHei";
    #endregion

    #region SC内容字体设置
    /// <summary>
    /// SC内容字体名称
    /// </summary>
    [ObservableProperty]
    private string _scContentFontFamily = "Microsoft YaHei";
    #endregion
}