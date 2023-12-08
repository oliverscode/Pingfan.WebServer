using System.Collections.Specialized;
using System.Text.Json;

namespace Pingfan.WebServer.Interfaces;

public interface IHttpRequest
{
    Uri Url { get; }
    string LocalPath { get; }
    string Method { get; }
    Stream InputStream { get; }
    NameValueCollection Headers { get; }
    string UserAgent { get; }
    string Ip { get; }

    string Device { get; }
    bool GetAuth(out string? userName, out string? password);

    
    string QueryString { get; }
    string PostString { get; }
    string Get(string key, string defaultValue = "");
    string Post(string key, string defaultValue = "");
    JsonDocument? Json { get; }

    string GetCookie(string key, string defaultValue = "");
    string this[string key] { get; }
}