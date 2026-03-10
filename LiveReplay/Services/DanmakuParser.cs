using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using LiveReplay.Models;

namespace LiveReplay.Services;

/// <summary>
/// 弹幕XML文件解析服务
/// 支持B站录播姬生成的XML格式
/// </summary>
public class DanmakuParser
{
    // 匹配头像URL的正则表达式：http/https开头，.jpg/.webp/.png/.gif结尾
    private static readonly Regex AvatarUrlRegex = new(
        @"https?://[^\s""']+?\.(?:jpg|webp|png|gif)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// 解析XML文件获取所有弹幕数据
    /// </summary>
    public async Task<(List<DanmakuItem> DanmakuList, List<ScItem> ScList, List<GiftItem> GiftList, DateTime StartTime)> ParseAsync(string xmlPath)
    {
        var doc = await Task.Run(() => XDocument.Load(xmlPath));

        var danmakuList = new List<DanmakuItem>();
        var scList = new List<ScItem>();
        var giftList = new List<GiftItem>();
        DateTime startTime = DateTime.Now;

        // 解析录制开始时间
        var recordInfo = doc.Root?.Element("BililiveRecorderRecordInfo");
        if (recordInfo != null)
        {
            var startTimeStr = recordInfo.Attribute("start_time")?.Value;
            if (!string.IsNullOrEmpty(startTimeStr) && DateTime.TryParse(startTimeStr, out var parsedTime))
            {
                startTime = parsedTime;
            }
        }

        // 解析弹幕
        foreach (var d in doc.Root?.Elements("d") ?? Enumerable.Empty<XElement>())
        {
            var danmaku = ParseDanmaku(d);
            if (danmaku != null)
            {
                danmakuList.Add(danmaku);
            }
        }

        // 解析SC
        foreach (var sc in doc.Root?.Elements("sc") ?? Enumerable.Empty<XElement>())
        {
            var scItem = ParseSc(sc, startTime);
            if (scItem != null)
            {
                scList.Add(scItem);
            }
        }

        // 解析礼物
        foreach (var gift in doc.Root?.Elements("gift") ?? Enumerable.Empty<XElement>())
        {
            var giftItem = ParseGift(gift);
            if (giftItem != null)
            {
                giftList.Add(giftItem);
            }
        }

        // 解析舰长(作为礼物处理)
        foreach (var guard in doc.Root?.Elements("guard") ?? Enumerable.Empty<XElement>())
        {
            var guardItem = ParseGuard(guard);
            if (guardItem != null)
            {
                giftList.Add(guardItem);
            }
        }

        // 按时间戳排序
        danmakuList.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        scList.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        giftList.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

        return (danmakuList, scList, giftList, startTime);
    }

    /// <summary>
    /// 从raw字符串中提取头像URL
    /// </summary>
    private string? ExtractAvatarUrl(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;

        // 尝试从JSON解析
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            // 如果是数组格式
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in root.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.Object)
                    {
                        var url = ExtractAvatarFromObject(element);
                        if (!string.IsNullOrEmpty(url)) return url;
                    }
                }
            }
            // 如果是对象格式
            else if (root.ValueKind == JsonValueKind.Object)
            {
                var url = ExtractAvatarFromObject(root);
                if (!string.IsNullOrEmpty(url)) return url;
            }
        }
        catch { }

        // 使用正则表达式从raw字符串中直接提取
        var match = AvatarUrlRegex.Match(raw);
        if (match.Success)
        {
            return match.Value;
        }

        return null;
    }

    /// <summary>
    /// 从JSON对象中提取头像URL
    /// </summary>
    private string? ExtractAvatarFromObject(JsonElement element)
    {
        // 尝试多种路径
        // 路径1: user.base.face
        if (element.TryGetProperty("user", out var userInfo))
        {
            if (userInfo.TryGetProperty("base", out var baseInfo))
            {
                if (baseInfo.TryGetProperty("face", out var face))
                {
                    return face.GetString();
                }
            }
        }

        // 路径2: uinfo.base.face
        if (element.TryGetProperty("uinfo", out var uinfo))
        {
            if (uinfo.TryGetProperty("base", out var baseInfo))
            {
                if (baseInfo.TryGetProperty("face", out var face))
                {
                    return face.GetString();
                }
            }
        }

        // 路径3: sender_uinfo.base.face
        if (element.TryGetProperty("sender_uinfo", out var senderInfo))
        {
            if (senderInfo.TryGetProperty("base", out var baseInfo))
            {
                if (baseInfo.TryGetProperty("face", out var face))
                {
                    return face.GetString();
                }
            }
        }

        // 路径4: 顶层face属性
        if (element.TryGetProperty("face", out var topFace))
        {
            return topFace.GetString();
        }

        return null;
    }

    private DanmakuItem? ParseDanmaku(XElement d)
    {
        try
        {
            var p = d.Attribute("p")?.Value;
            var user = d.Attribute("user")?.Value;
            var raw = d.Attribute("raw")?.Value;
            var content = d.Value;

            if (string.IsNullOrEmpty(p)) return null;

            var pParts = p.Split(',');
            var timestamp = double.Parse(pParts[0]);

            var item = new DanmakuItem
            {
                Timestamp = timestamp,
                UserName = user ?? "",
                Content = content
            };

            // 获取用户ID
            if (pParts.Length > 6)
            {
                item.UserId = long.Parse(pParts[6]);
            }

            // 获取颜色
            if (pParts.Length > 2)
            {
                item.Color = int.Parse(pParts[2]);
            }

            // 提取头像URL
            item.AvatarUrl = ExtractAvatarUrl(raw) ?? "";

            return item;
        }
        catch
        {
            return null;
        }
    }

    private ScItem? ParseSc(XElement sc, DateTime startTime)
    {
        try
        {
            var ts = sc.Attribute("ts")?.Value;
            var user = sc.Attribute("user")?.Value;
            var uid = sc.Attribute("uid")?.Value;
            var price = sc.Attribute("price")?.Value;
            var time = sc.Attribute("time")?.Value;
            var raw = sc.Attribute("raw")?.Value;
            var content = sc.Value;

            if (string.IsNullOrEmpty(ts)) return null;

            var timestamp = double.Parse(ts);

            var item = new ScItem
            {
                Timestamp = timestamp,
                UserName = user ?? "",
                UserId = long.TryParse(uid, out var uidValue) ? uidValue : 0,
                Price = int.TryParse(price, out var priceValue) ? priceValue : 0,
                Duration = int.TryParse(time, out var timeValue) ? timeValue : 60,
                Content = content,
                SendTime = startTime.AddSeconds(timestamp)
            };

            // 提取头像URL
            item.AvatarUrl = ExtractAvatarUrl(raw) ?? "";

            return item;
        }
        catch
        {
            return null;
        }
    }

    private GiftItem? ParseGift(XElement gift)
    {
        try
        {
            var ts = gift.Attribute("ts")?.Value;
            var user = gift.Attribute("user")?.Value;
            var uid = gift.Attribute("uid")?.Value;
            var giftName = gift.Attribute("giftname")?.Value;
            var giftCount = gift.Attribute("giftcount")?.Value;
            var raw = gift.Attribute("raw")?.Value;

            if (string.IsNullOrEmpty(ts)) return null;

            var timestamp = double.Parse(ts);

            var item = new GiftItem
            {
                Timestamp = timestamp,
                UserName = user ?? "",
                UserId = long.TryParse(uid, out var uidValue) ? uidValue : 0,
                GiftName = giftName ?? "",
                GiftCount = int.TryParse(giftCount, out var countValue) ? countValue : 1,
                IsGuard = false
            };

            // 解析价格和头像
            if (!string.IsNullOrEmpty(raw))
            {
                try
                {
                    using var doc = JsonDocument.Parse(raw);
                    var root = doc.RootElement;

                    // 获取价格
                    if (root.TryGetProperty("price", out var priceElement))
                    {
                        item.Price = priceElement.GetInt32() / 1000.0;
                    }
                    else if (root.TryGetProperty("total_coin", out var totalCoin))
                    {
                        item.Price = totalCoin.GetInt32() / 1000.0;
                    }
                }
                catch { }

                // 提取头像URL
                item.AvatarUrl = ExtractAvatarUrl(raw) ?? "";
            }

            return item;
        }
        catch
        {
            return null;
        }
    }

    private GiftItem? ParseGuard(XElement guard)
    {
        try
        {
            var ts = guard.Attribute("ts")?.Value;
            var user = guard.Attribute("user")?.Value;
            var uid = guard.Attribute("uid")?.Value;
            var level = guard.Attribute("level")?.Value;
            var count = guard.Attribute("count")?.Value;
            var raw = guard.Attribute("raw")?.Value;

            if (string.IsNullOrEmpty(ts)) return null;

            var timestamp = double.Parse(ts);
            var guardLevel = int.TryParse(level, out var levelValue) ? levelValue : 3;

            var guardNames = new Dictionary<int, string>
            {
                { 1, "总督" },
                { 2, "提督" },
                { 3, "舰长" }
            };

            var item = new GiftItem
            {
                Timestamp = timestamp,
                UserName = user ?? "",
                UserId = long.TryParse(uid, out var uidValue) ? uidValue : 0,
                GiftName = guardNames.GetValueOrDefault(guardLevel, "舰长"),
                GiftCount = int.TryParse(count, out var countValue) ? countValue : 1,
                IsGuard = true,
                GuardLevel = guardLevel,
                Price = guardLevel switch
                {
                    1 => 19998,
                    2 => 1998,
                    _ => 198
                }
            };

            // 解析raw获取价格和头像
            if (!string.IsNullOrEmpty(raw))
            {
                try
                {
                    using var doc = JsonDocument.Parse(raw);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("price", out var priceElement))
                    {
                        item.Price = priceElement.GetInt32() / 1000.0;
                    }
                }
                catch { }

                item.AvatarUrl = ExtractAvatarUrl(raw) ?? "";
            }

            return item;
        }
        catch
        {
            return null;
        }
    }
}