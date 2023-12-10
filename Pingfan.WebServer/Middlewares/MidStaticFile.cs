using System.Text.RegularExpressions;
using Pingfan.Inject;
using Pingfan.WebServer.Interfaces;

namespace Pingfan.WebServer.Middlewares;

/// <summary>
/// 静态文件中间件
/// </summary>
public class MidStaticFile : IMiddleware
{
    private readonly List<KeyValuePair<string, string>> _wwwRoot =
        new List<KeyValuePair<string, string>>();

    private readonly HashSet<string> _defaultFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "index.html",
        "default.html",
    };

    // 常见的mime
    private static readonly Dictionary<string, string> _mime =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".html", "text/html" },
            { ".htm", "text/html" },
            { ".js", "application/javascript" },
            { ".css", "text/css" },
            { ".png", "image/png" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".gif", "image/gif" },
            { ".ico", "image/x-icon" },
            { ".svg", "image/svg+xml" },
            { ".ttf", "font/ttf" },
            { ".woff", "font/woff" },
            { ".woff2", "font/woff2" },
            { ".eot", "application/vnd.ms-fontobject" },
            { ".otf", "font/otf" },
            { ".mp3", "audio/mpeg" },
            { ".mp4", "video/mp4" },
            { ".ts", "video/mp2t" },
            { ".webm", "video/webm" },
            { ".wav", "audio/wav" },
            { ".ogg", "audio/ogg" },
            { ".zip", "application/zip" },
            { ".rar", "application/x-rar-compressed" },
            { ".7z", "application/x-7z-compressed" },
            { ".pdf", "application/pdf" },
            { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".xls", "application/vnd.ms-excel" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".ppt", "application/vnd.ms-powerpoint" },
            { ".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation" },
            { ".json", "application/json" },
            { ".txt", "text/plain" },
        };


    // /// <summary>
    // /// 最大速度, 默认10M/s
    // /// </summary>
    // public long MaxSpeed { get; set; } = 1024 * 100; // 1024 * 1024 * 10;


    /// <summary>
    /// 添加一个静态文件映射目录 http://localhost/{prefix}/xxx.html => {dir}/xxx.html
    /// </summary>
    /// <param name="prefix">前缀, 一般写/即可</param>
    /// <param name="dir">本地绝对目录</param>
    public bool AddDirectory(string prefix, string dir)
    {
        if (dir.EndsWith(Path.DirectorySeparatorChar.ToString()) == false)
        {
            dir += Path.DirectorySeparatorChar;
        }

        _wwwRoot.Add(new KeyValuePair<string, string>(prefix, dir));
        return true;
    }

    /// <summary>
    /// 添加一个默认首页文件
    /// </summary>
    /// <param name="file"></param>
    public void AddDefaultFile(string file)
    {
        _defaultFiles.Add(file);
    }

    /// <summary>
    /// 获取扩展名对应的MIME
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static string GetMime(string key)
    {
        key = Path.GetExtension(key);
        if (_mime.TryGetValue(key, out var mime))
            return mime;

        var item = _mime.FirstOrDefault(p => p.Key.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0);
        if (item.Value != null)
            return item.Value;
        return "application/octet-stream";
    }

    /// <summary>
    /// 设置扩展名对应的MIME
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public static void SetMime(string key, string value)
    {
        _mime.TryAdd(key, value);
    }


    public void Invoke(IContainer container, IHttpContext ctx, Action next)
    {
        var url = ctx.Request.Path.EndsWith("/") ? "" : ctx.Request.Path;

        // 判断本地是否存在这个文件
        foreach (var path in _wwwRoot)
        {
            if (url.StartsWith(path.Key) == false)
                continue;

            var fileName = url.Substring(path.Key.Length);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                foreach (var defaultFile in _defaultFiles)
                {
                    var localPath = Path.Combine(path.Value, ctx.Request.Path, defaultFile);

                    // 存在就不继续了
                    if (File.Exists(localPath))
                    {
                        WriteTo(localPath, ctx);
                        return;
                    }
                }
            }
            else
            {
                var localPath = Path.Combine(path.Value, fileName);

                // 存在就不继续了
                if (File.Exists(localPath))
                {
                    WriteTo(localPath, ctx);
                    return;
                }
            }
        }

        // 找不到静态文件, 执行后续
        next();
    }

    private void WriteTo(string path, IHttpContext ctx)
    {
        long fileSize = new FileInfo(path).Length;
        long start = 0, end = fileSize - 1;
        int statusCode = 200;


        var range = ctx.Request.Headers["Range"];
        if (range != null)
        {
            Match match = Regex.Match(range, @"bytes=(\d*)-(\d*)");
            if (match.Success)
            {
                if (!string.IsNullOrEmpty(match.Groups[1].Value))
                {
                    start = long.Parse(match.Groups[1].Value);
                    statusCode = 206;
                }

                if (!string.IsNullOrEmpty(match.Groups[2].Value))
                {
                    end = long.Parse(match.Groups[2].Value);
                    statusCode = 206;
                }
            }
        }

        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = GetMime(path);


        if (statusCode == 206)
        {
            ctx.Response.Headers["Content-Range"] = $"bytes {start}-{end}/{fileSize}";
            ctx.Response.Headers["Accept-Ranges"] = "bytes";
            // ctx.Response.Headers["Content-Length"] = (end - start + 1).ToString();

            ctx.Response.HttpListenerContext.Response.ContentLength64 = (end - start + 1);
        }
        else
        {
            // ctx.Response.Headers["Content-Length"] = fileSize.ToString();
            // ctx.Response.HttpListenerContext.Response.ContentLength64 = fileSize;
        }

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        fs.Seek(start, SeekOrigin.Begin);

        byte[] buffer = new byte[1024 * 64]; // 64KB buffer
        long bytesToRead = end - start + 1;
        while (bytesToRead > 0)
        {
            int bytesRead = fs.Read(buffer, 0, (int)Math.Min(bytesToRead, buffer.Length));
            if (bytesRead == 0)
            {
                break;
            }

            ctx.Response.HttpListenerContext.Response.OutputStream.Write(buffer, 0, bytesRead);
            bytesToRead -= bytesRead;
        }
    }
}