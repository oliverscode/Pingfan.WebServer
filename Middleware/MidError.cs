using System;

namespace Pingfan.WebServer.Middleware
{

    /// <summary>
    /// 错误中间件
    /// </summary>
    public class MidError : IMiddleware
    {
        /// <summary>
        /// 是否向客户端返回错误信息
        /// </summary>
        public bool ShowError { get; set; } = true;

        public MidError()
        {
        }

        /// <summary>
        /// 可默认配置一些参数
        /// </summary>
        /// <param name="fn"></param>
        public MidError(Action<MidError> fn)
        {
            fn(this);
        }

        public event Action<HttpContext, Exception> OnError;

        public void Invoke(HttpContext ctx, Action<HttpContext> next)
        {
            try
            {
                next(ctx);
            }
            catch (HttpEndException e)
            {
            }
            catch (Exception e)
            {
                if (ctx.Response.StatusCode < 500)
                    ctx.Response.StatusCode = 500;
                if (ShowError)
                {
                    ctx.Response.Write(e.ToString());
                }
                OnError?.Invoke(ctx, e);

            }
        }
    }
}