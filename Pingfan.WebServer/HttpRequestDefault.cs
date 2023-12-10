using System.Collections.Specialized;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Pingfan.WebServer.Interfaces;

namespace Pingfan.WebServer;

public class HttpRequestDefault : IHttpRequest
{
    public HttpListenerContext HttpListenerContext { get; }
    public HttpListenerRequest HttpListenerRequest => HttpListenerContext.Request;

    public HttpRequestDefault(HttpListenerContext httpListenerContext)
    {
        HttpListenerContext = httpListenerContext;
    }

    public void Dispose()
    {
        try
        {
            HttpListenerContext.Request.InputStream.Dispose();
            InputStream.Dispose();
        }
        catch
        {
            //
        }
    }


    public Uri Url => HttpListenerRequest.Url!;

    private string _path = null!;

    public string Path
    {
        get
        {
            if (string.IsNullOrEmpty(_path))
            {
                var path = Regex.Replace(Url.LocalPath, "//", "/");
                path = Regex.Replace(path, @"\\", @"\");
                _path = path;
            }

            return _path;
        }
        set => _path = value;
    }

    public string Method => HttpListenerRequest.HttpMethod.ToUpper();

    public Stream InputStream => HttpListenerRequest.InputStream;

    public NameValueCollection Headers => HttpListenerRequest.Headers;
    public string UserAgent => HttpListenerRequest.UserAgent;


    private string? _ip;

    public string Ip
    {
        get
        {
            if (!string.IsNullOrEmpty(_ip)) return _ip;
            string[] ipHeads = { "CF-Connecting-IP", "X_FORWARDED_FOR", "X-Forwarded-For", "X-Real-IP" };
            var ips = ipHeads.Select(head => HttpListenerRequest.Headers[head])
                .Where(t => string.IsNullOrEmpty(t) == false)
                .ToList();

            ips.Add(HttpListenerRequest.RemoteEndPoint.Address.ToString());

            // 只需要第一个IP
            foreach (var m in ips.Select(ip => Regex.Match(ip!, @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})"))
                         .Where(m => m.Success))
            {
                _ip = m.Groups[1].Value;
                break;
            }

            return _ip;
        }
    }

    private string? _device;

    public string Device
    {
        get
        {
            if (!string.IsNullOrEmpty(_device)) return _device;
            if (HttpListenerRequest.UserAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
                _device = "Android";
            else if (HttpListenerRequest.UserAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase))
                _device = "iPhone";
            else if (HttpListenerRequest.UserAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
                _device = "iPad";
            else if (HttpListenerRequest.UserAgent.Contains("Windows Phone", StringComparison.OrdinalIgnoreCase))
                _device = "Windows Phone";
            else if (HttpListenerRequest.UserAgent.Contains("Windows NT", StringComparison.OrdinalIgnoreCase))
                _device = "Windows";
            else if (HttpListenerRequest.UserAgent.Contains("Mac OS", StringComparison.OrdinalIgnoreCase))
                _device = "Mac OS";
            else if (HttpListenerRequest.UserAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
                _device = "Linux";
            else _device = "Other";

            return _device;
        }
    }


    private string? _queryString;

    public string QueryString
    {
        get
        {
            if (string.IsNullOrEmpty(_queryString) == false) return _queryString;
            if (HttpListenerRequest.Url == null) return "";

            _queryString = HttpListenerRequest.Url.Query;
            if (_queryString.StartsWith("?"))
                _queryString = _queryString[1..];

            return _queryString;
        }
    }

    private string? _postString;

    public string Body
    {
        get
        {
            if (_postString != null) return _postString;
            using var sr = new StreamReader(HttpListenerRequest.InputStream);
            _postString = sr.ReadToEnd();
            return _postString;
        }
        set => _postString = value;
    }

    public CookieCollection Cookies => HttpListenerRequest.Cookies;

    public string? Get(string key)
    {
        return GetValue(QueryString, key)!;
    }

    public string? Form(string key)
    {
        return GetValue(Body, key)!;
    }

    private JsonDocument? _json;

    public JsonDocument? Json
    {
        get
        {
            if (_json != null) return _json;
            if (string.IsNullOrEmpty(Body)) return null;
            try
            {
                _json = JsonDocument.Parse(Body);
                return _json;
            }
            catch
            {
                return null;
            }
        }
    }

    public string? GetCookie(string key)
    {
        
        var cookie = HttpListenerRequest.Cookies[key];
        return cookie?.Value;
    }

    public string? this[string key]
    {
        // post->get->cookie->header
        get
        {
            var value = ((Form(key) ?? Get(key)) ?? GetCookie(key)) ?? Headers[key];
            return value;
        }
    }

    private static string? GetValue(string? source, string key)
    {
        if (string.IsNullOrEmpty(source)) return null;
        var m = Regex.Match(source, $@"{key}=(?<value>[^&]*)");
        return m.Success ? m.Groups["value"].Value : null;
    }
}