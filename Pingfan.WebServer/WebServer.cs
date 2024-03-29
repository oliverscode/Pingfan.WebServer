﻿using System.Collections.Concurrent;
using System.Net;
using System.Runtime.InteropServices;
using Pingfan.Inject;
using Pingfan.WebServer.Interfaces;

namespace Pingfan.WebServer;

public class WebServer : IContainerReady
{
    private readonly HttpListener _httpListener = new HttpListener();
    private readonly List<IMiddleware> _middlewares = new List<IMiddleware>();

    private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    // // HTTP路由列表
    // private readonly ConcurrentDictionary<string, Action<IHttpContext>> _httpMaps =
    //     new(StringComparer.OrdinalIgnoreCase);


    [Inject] public IContainer Container { get; set; } = null!;
    public WebServerConfig Config { get; }


    /// <summary>
    /// 请求开始前执行
    /// </summary>
    public event Action<IHttpContext>? BeginRequest;

    /// <summary>
    /// 仅仅在BeginRequest之后执行
    /// </summary>
    public event Action<IHttpContext>? Handler;


    /// <summary>
    /// 请求结束后执行
    /// </summary>
    public event Action<IHttpContext>? EndRequest;

    /// <summary>
    /// 有异常时执行
    /// </summary>
    public event Action<IHttpContext, Exception>? RequestError;


    public WebServer(WebServerConfig config)
    {
        Config = config;

        _httpListener.Prefixes.Add($"http://*:{config.Port}/");
        _httpListener.Start();
    }

    public void OnContainerReady()
    {
        StartListen();
    }


    // 开始循环获取请求
    private async void StartListen()
    {
        while (true)
        {
            var httpListenerContext = await _httpListener.GetContextAsync();
            ThreadPool.QueueUserWorkItem((obj) =>
            {
                var context = (HttpListenerContext)obj!;
                ExecuteHttpContext(context);
            }, httpListenerContext);
        }
        // ReSharper disable once FunctionNeverReturns
    }

    // 正式执行流程
    private void ExecuteHttpContext(HttpListenerContext httpListenerContext)
    {
        var httpContainer = Container.CreateContainer();
        httpContainer.Push<IHttpContext>(Config.HttpContextType);
        httpContainer.Push<IHttpRequest>(Config.HttpRequestType);
        httpContainer.Push<IHttpResponse>(Config.HttpResponseType);

        var items = new Dictionary<string, object>();
        httpContainer.Push(items);


        httpContainer.Push(httpListenerContext);

        var httpContext = (IHttpContext)httpContainer.Get(Config.HttpContextType)!;

        try
        {
            if (IsWindows)
                httpListenerContext.Response.Headers["Server"] = "";
            else
                httpListenerContext.Response.Headers.Add("Server", Config.DefaultServerName);
            httpListenerContext.Response.Headers["Date"] = "";


            // 如果http协议大于1.1 就启用SendChunked
            // if (httpContext.Request.HttpListenerContext.Request.ProtocolVersion >= HttpVersion.Version11)
            // {
            //     httpListenerContext.Response.SendChunked = true;
            //     httpContext.Response.KeepAlive = true;
            // }

            this.BeginRequest?.Invoke(httpContext);


            this.Handler?.Invoke(httpContext);

            // 执行中间件
            var midIndex = 0;
            Action? func = null;
            func = () =>
            {
                if (midIndex < _middlewares.Count)
                {
                    _middlewares[midIndex++].Invoke(httpContainer, httpContext, func!);
                }
            };
            func();


            this.EndRequest?.Invoke(httpContext);
        }
        catch (HttpEndException)
        {
        }
        catch (HttpArgumentException)
        {
        }
        catch (Exception ex)
        {
            if (httpContext.Response.StatusCode < 500)
                httpContext.Response.StatusCode = 500;
            this.RequestError?.Invoke(httpContext, ex);
        }
        finally
        {
            httpContainer.Dispose();
        }
    }

    public void UseMiddleware(IMiddleware middleware)
    {
        _middlewares.Add(middleware);
    }

    // /// <summary>
    // /// 映射一个路由
    // /// </summary>
    // /// <param name="url"></param>
    // /// <param name="fn"></param>
    // public void Map(string url, Action<IHttpContext> fn)
    // {
    //     _httpMaps[url] = fn;
    // }
}

public static class WebServerExtensions
{
    public static WebServer UseWebServer(this IContainer container, Action<WebServerConfig> func)
    {
        var config = new WebServerConfig();
        func(config);
        container.Push(config);


        return container.New<WebServer>();
    }
}