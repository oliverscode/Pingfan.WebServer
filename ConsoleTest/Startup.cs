﻿using Pingfan.Inject;
using Pingfan.WebServer;
using Pingfan.WebServer.Interfaces;
using Pingfan.WebServer.Middlewares;

namespace ConsoleTest;

/// <inheritdoc />
public class Startup : IContainerReady
{
    [Inject] public IContainer Container { get; set; } = null!;

    public void OnContainerReady()
    {
        var webServer = Container.UseWebServer(config => { config.Port = 8080; });


        var error = new MidError();
        error.OnError += (ctx, err) => { Console.WriteLine("发生错误" + err); };
        webServer.UseMiddleware(error);

        
        var webSocket = Container.New<MidWebSocket>();
        webSocket.Add<Game>("/");
        webServer.UseMiddleware(webSocket);
       
        

        var staticFile = Container.New<MidStaticFile>();
        staticFile.AddDirectory("/", "D:\\露营照片");
        staticFile.AddDirectory("/", "E:\\");
        staticFile.AddDirectory("/", "www");
        webServer.UseMiddleware(staticFile);


        var log = Container.New<MidLog>();
        log.HttpMethod = "GET,POST";
        log.LogHandler = Console.WriteLine;
        webServer.UseMiddleware(log);


        var api = this.Container.New<MidApi>();
        api.Add<Home>();
        webServer.UseMiddleware(api);


        // webServer.BeginRequest += (ctx) =>
        // {
        //     ctx.Response.SendChunked = false;
        //     ctx.Response.Write("Hello World");
        // };
        webServer.RequestError += (ctx, ex) =>
        {
            if (ex is InjectNotRegisteredException injectNotRegisteredException)
            {
                ctx.Response.OutputStream.SetLength(0);
                ctx.Response.StatusCode = 500;
                ctx.Response.Write($"参数错误{injectNotRegisteredException.Pop.Name}");
            }
        };

        Console.WriteLine("启动成功");
    }
}