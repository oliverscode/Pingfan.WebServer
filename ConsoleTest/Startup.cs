using Pingfan.Inject;
using Pingfan.WebServer;
using Pingfan.WebServer.Interfaces;

namespace ConsoleTest;

public class Startup : IContainerReady
{
    [Inject] public IContainer Container { get; set; } = null!;

    public void OnContainerReady()
    {
        var webServer = Container.UseWebServer(config => { config.Port = 8080; });
        // webServer.BeginRequest += (ctx) =>
        // {
        //     ctx.Response.SendChunked = false;
        //     ctx.Response.Write("Hello World");
        // };
    }
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