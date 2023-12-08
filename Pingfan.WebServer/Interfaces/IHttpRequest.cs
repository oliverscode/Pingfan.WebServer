using System.Collections.Specialized;
using System.Net;
using System.Text.Json;

namespace Pingfan.WebServer.Interfaces;

public interface IHttpRequest : IDisposable
{
    protected internal HttpListenerRequest ListenerRequest { get; }

    Uri? Url { get; }
    string? LocalPath { get; }
    string Method { get; }
    Stream InputStream { get; }
    NameValueCollection Headers { get; }
    string UserAgent { get; }
    string? Ip { get; }
    string? Device { get; }
    string? QueryString { get; }
    string? PostString { get; }
    string Get(string key, string defaultValue = "");
    string Post(string key, string defaultValue = "");
    JsonDocument? Json { get; }

    string GetCookie(string key, string defaultValue = "");
    string this[string key] { get; }
}