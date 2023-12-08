
using Pingfan.Inject;
using Pingfan.WebServer;
using Pingfan.WebServer.Interfaces;

namespace ConsoleTest;

public class Startup : IContainerReady
{
    [Inject] public IContainer Container { get; set; } = null!;

    public void OnContainerReady()
    {
        Container.UseWebServer(config => { config.Port = 8080; });
    }
}

public static class WebServerExtensions
{
    public static void UseWebServer(this IContainer container, Action<WebServerConfig> func)
    {
        var config = new WebServerConfig();
        func(config);
        container.Push(config);
        
        
        container.New<WebServer>();
    }
}