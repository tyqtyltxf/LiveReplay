using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace LiveReplay.Helpers;

/// <summary>
/// 头像图片加载帮助类
/// 支持: jpg, png, gif, webp (webp会通过B站CDN参数转换为jpg)
/// </summary>
public static class AvatarLoader
{
    private static readonly HttpClient _httpClient = new();
    private static readonly string _cacheDir = Path.Combine(Path.GetTempPath(), "LiveReplay_AvatarCache");

    static AvatarLoader()
    {
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        _httpClient.DefaultRequestHeaders.Add("Referer", "https://www.bilibili.com/");

        if (!Directory.Exists(_cacheDir))
        {
            Directory.CreateDirectory(_cacheDir);
        }
    }

    /// <summary>
    /// 异步加载头像图片
    /// 支持 http/https 开头，.jpg/.png/.gif/.webp 结尾的URL
    /// </summary>
    public static async Task<BitmapImage?> LoadAvatarAsync(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        // 验证URL格式
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            // 如果是B站API URL，需要先获取头像URL
            if (url.Contains("api.bilibili.com"))
            {
                return await LoadFromBilibiliApi(url);
            }

            // 直接是图片URL
            return await LoadFromImageUrl(url);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 判断URL是否为WebP格式
    /// </summary>
    private static bool IsWebPUrl(string url)
    {
        // 检查URL路径部分是否以.webp结尾（忽略查询参数）
        var pathEnd = url.IndexOf('?');
        var path = pathEnd > 0 ? url.Substring(0, pathEnd) : url;
        return path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 判断URL是否为PNG格式
    /// </summary>
    private static bool IsPngUrl(string url)
    {
        var pathEnd = url.IndexOf('?');
        var path = pathEnd > 0 ? url.Substring(0, pathEnd) : url;
        return path.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 将B站图片URL转换为可加载格式
    /// WebP: 添加@参数请求JPG格式
    /// PNG/JPG/GIF: 直接加载
    /// </summary>
    private static string GetLoadableUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;

        // 只处理B站图片
        if (!url.Contains("hdslb.com") && !url.Contains("bilibili.com"))
            return url;

        // 移除可能存在的@参数
        var atIndex = url.IndexOf('@');
        if (atIndex > 0)
        {
            url = url.Substring(0, atIndex);
        }

        // WebP格式: B站CDN支持通过@参数转换格式
        if (IsWebPUrl(url))
        {
            // B站CDN格式转换: @100w_100h_1c_1s.jpg 表示100x100的jpg图片
            // 使用.jpg后缀让服务器返回JPG格式
            return url + "@100w_100h_1c_1s.jpg";
        }

        // PNG格式: 也使用@参数确保能加载
        if (IsPngUrl(url))
        {
            return url + "@100w_100h_1c_1s.jpg";
        }

        // JPG/GIF: 添加合适的尺寸参数
        if (url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return url + "@100w_100h_1c_1s.jpg";
        }

        // GIF保持原样
        return url;
    }

    /// <summary>
    /// 获取URL对应的缓存文件路径
    /// </summary>
    private static string GetCachePath(string url)
    {
        var hash = Math.Abs(url.GetHashCode()).ToString("x");

        // 根据URL判断扩展名
        var ext = ".jpg";
        if (url.Contains(".jpg") || url.Contains(".jpeg"))
            ext = ".jpg";
        else if (url.Contains(".png"))
            ext = ".png";
        else if (url.Contains(".gif"))
            ext = ".gif";
        else if (url.Contains(".webp"))
            ext = ".jpg"; // webp会转换为jpg

        return Path.Combine(_cacheDir, $"{hash}{ext}");
    }

    private static async Task<BitmapImage?> LoadFromBilibiliApi(string apiUrl)
    {
        try
        {
            var json = await _httpClient.GetStringAsync(apiUrl);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("data", out var data) &&
                data.TryGetProperty("face", out var face))
            {
                var avatarUrl = face.GetString();
                if (!string.IsNullOrEmpty(avatarUrl))
                {
                    return await LoadFromImageUrl(avatarUrl);
                }
            }
        }
        catch
        {
            // 忽略错误
        }
        return null;
    }

    private static async Task<BitmapImage?> LoadFromImageUrl(string url)
    {
        // 获取可加载的URL（处理webp/png格式）
        var loadableUrl = GetLoadableUrl(url);

        // 尝试加载转换后的URL
        var result = await TryLoadUrl(loadableUrl);
        if (result != null)
            return result;

        // 如果转换后的URL加载失败，尝试原始URL
        if (loadableUrl != url)
        {
            result = await TryLoadUrl(url);
            if (result != null)
                return result;
        }

        return null;
    }

    /// <summary>
    /// 尝试从URL加载图片
    /// </summary>
    private static async Task<BitmapImage?> TryLoadUrl(string url)
    {
        try
        {
            var cachePath = GetCachePath(url);

            // 如果缓存存在，直接使用
            if (File.Exists(cachePath))
            {
                return await LoadFromLocalAsync(cachePath);
            }

            // 下载图片
            var imageBytes = await _httpClient.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(cachePath, imageBytes);

            return await LoadFromBytesAsync(imageBytes);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<BitmapImage?> LoadFromLocalAsync(string path)
    {
        return await Task.Run(() =>
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        });
    }

    private static async Task<BitmapImage?> LoadFromBytesAsync(byte[] bytes)
    {
        return await Task.Run(() =>
        {
            try
            {
                var bitmap = new BitmapImage();
                using var stream = new MemoryStream(bytes);
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        });
    }
}