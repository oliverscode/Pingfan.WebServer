﻿using System.Text;
using Pingfan.Inject;
using Pingfan.WebServer.Interfaces;

namespace Pingfan.WebServer.Middlewares;

/// <summary>
/// 日志中间件
/// </summary>
public class MidLog : IMiddleware
{
    /// <summary>
    /// 写入日志回调方法
    /// </summary>
    public Action<string>? LogHandler { get; set; }


    /// <summary>
    /// 要记录日志的请求类型, 用逗号分割,例如:POST,GET, 默认只记录POST
    /// </summary>
    public string HttpMethod { get; set; } = "POST";

    /// <summary>
    /// 是否输出日志到磁盘, 默认写入到磁盘
    /// </summary>
    public bool IsWriteDisk { get; set; } = true;


    /// <summary>
    /// 中间件的执行方法
    /// </summary>
    public void Invoke(IContainer container, IHttpContext ctx, Action next)
    {
        // 不记录日志的请求类型
        if (!HttpMethod.Contains(ctx.Request.Method, StringComparison.OrdinalIgnoreCase))
        {
            next();
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {ctx.Request.Ip}");
        sb.AppendLine($"{ctx.Request.Method} {ctx.Request.Url}");
        foreach (var key in ctx.Request.Headers.AllKeys)
        {
            var value = ctx.Request.Headers[key];
            sb.AppendLine($"{key}={value}");
        }

        sb.AppendLine("Body");
        sb.AppendLine(ctx.Request.Body);
        sb.AppendLine();

        // 执行后续中间件
        next();

        // 响应的部分
        sb.AppendLine($"Status Code: {ctx.Response.StatusCode}");
        foreach (var key in ctx.Response.Headers.AllKeys)
        {
            var value = ctx.Response.Headers[key];
            if (string.IsNullOrWhiteSpace(value) == false)
                sb.AppendLine($"{key}={value}");
        }

        sb.AppendLine("Body");


        var stream = new MemoryStream();
        ctx.Response.OutputStream.WriteTo(stream);
        var bodyString = ctx.Response.ContentEncoding.GetString(stream.ToArray());


        sb.AppendLine(bodyString);
        sb.AppendLine();
        sb.AppendLine();

        var log = sb.ToString();

        LogHandler?.Invoke(log);
        if (IsWriteDisk)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "access", $"{DateTime.Now:yyyy-MM-dd}.log");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);            
            File.AppendAllText(path, log);

            // // 写入日志
            // if (ctx.Response.StatusCode >= 200 && ctx.Response.StatusCode < 400)
            // {
            //     File.AppendAllText(
            //         Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{DateTime.Now:yyyy-MM-dd} access.log"),
            //         log);
            // }
            // else if (ctx.Response.StatusCode >= 400 && ctx.Response.StatusCode < 500)
            // {
            //     File.AppendAllText(
            //         Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{DateTime.Now:yyyy-MM-dd} notfound.log"),
            //         log);
            // }
            // else if (ctx.Response.StatusCode >= 500 && ctx.Response.StatusCode < 600)
            // {
            //     File.AppendAllText(
            //         Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{DateTime.Now:yyyy-MM-dd} error.log"),
            //         log);
            // }
        }
    }
}