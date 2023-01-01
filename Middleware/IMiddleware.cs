using System;

namespace PingFan.WebServer.Middleware
{


    /// <summary>
    /// 所有中间件的根接口
    /// </summary>
    public interface IMiddleware
    {
        void Invoke(HttpContext ctx, Action<HttpContext> next);
    }
}