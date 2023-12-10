using Pingfan.Inject;

namespace Pingfan.WebServer.Interfaces;

public interface IMiddleware
{
    void Invoke(IContainer container, IHttpContext ctx, Action next);
}