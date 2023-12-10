using Pingfan.WebServer.Interfaces;

namespace Pingfan.WebServer;

public class HttpContextDefault : IHttpContext
{
    public IHttpRequest Request { get; }
    public IHttpResponse Response { get; }
    public Dictionary<string, object> Items { get; }
    public void NextMiddleware()
    {
        
    }

    public HttpContextDefault(IHttpRequest request, IHttpResponse response, Dictionary<string, object> items)
    {
        Request = request;
        Response = response;
        Items = items;
    }

    public void Dispose()
    {
        Request.Dispose();
        Response.Dispose();
        Items.Clear();
        
    }
}