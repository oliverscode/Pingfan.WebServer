﻿using System.Collections.Specialized;
using System.Net;
using System.Text.Json;

namespace Pingfan.WebServer.Interfaces;

public interface IHttpRequest : IDisposable
{
    protected internal HttpListenerContext HttpListenerContext { get; }

    Uri? Url { get; }
    string Path { get; }
    string Method { get; }
    Stream InputStream { get; }
    NameValueCollection Headers { get; }
    string UserAgent { get; }
    string Ip { get; }
    string Device { get; }
    string QueryString { get; }
    string Body { get; }
    CookieCollection Cookies { get; }
    string? Get(string key);
    string? Form(string key);
    JsonDocument? Json { get; }

    string? GetCookie(string key);
    string? this[string key] { get; }
}