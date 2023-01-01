using System;
using System.IO;
using System.Text;
using Pingfan.Kit;

namespace PingFan.WebServer.Middleware
{


    public class MidLog: IMiddleware
    {
        /// <summary>
        /// 写入日志回调方法
        /// </summary>
        public Action<string> OnLogHandler { get; set; }


        public MidLog()
        {
        }

        /// <summary>
        /// 是否输出日志到磁盘, 默认写入到磁盘
        /// </summary>
        public bool IsWriteDisk { get; set; } = true;

        /// <summary>
        /// 可默认配置一些参数
        /// </summary>
        /// <param name="fn"></param>
        public MidLog(Action<MidLog> fn)
        {
            fn(this);
        }


        public void Invoke(HttpContext ctx, Action<HttpContext> next)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{DateTime.Now.ToDateTimeString()}");
            sb.AppendLine($"{ctx.Request.Method.ToUpper()} {ctx.Request.QueryString}");
            foreach (var key in ctx.Request.Headers.AllKeys)
            {
                var value = ctx.Request.Headers[key];
                sb.AppendLine($"{key}={value}");
            }

            sb.AppendLine("Body");
            sb.AppendLine(ctx.Request.PostString);
            sb.AppendLine();

            // 执行后续中间件
            next(ctx);

            // 响应的部分
            sb.AppendLine($"Status Code: {ctx.Response.StatusCode}");
            foreach (var key in ctx.Response.Headers.AllKeys)
            {
                var value = ctx.Response.Headers[key];
                if (value.IsNullOrWhiteSpace() == false)
                    sb.AppendLine($"{key}={value}");
            }

            sb.AppendLine("Body");
            var bodyString = ctx.Response.BodyString;
            sb.AppendLine(bodyString);
            sb.AppendLine();
            sb.AppendLine();

            var log = sb.ToString();
            if (OnLogHandler != null)
            {
                OnLogHandler(log);
                return;
            }

            if (IsWriteDisk)
            {
                // 写入日志
                if (ctx.Response.StatusCode >= 200 && ctx.Response.StatusCode < 400)
                {
                    FileEx.AppendAllText(PathEx.Combine(PathEx.CurrentDirectory,$"{DateTime.Now:yyyy-MM-dd}access.log"), log);
                }
                else if (ctx.Response.StatusCode >= 400 && ctx.Response.StatusCode < 500)
                {
                    FileEx.AppendAllText(PathEx.Combine(PathEx.CurrentDirectory,$"{DateTime.Now:yyyy-MM-dd}notfound.log"), log);
                }
                else if (ctx.Response.StatusCode >= 500 && ctx.Response.StatusCode < 600)
                {
                    FileEx.AppendAllText(PathEx.Combine(PathEx.CurrentDirectory,$"{DateTime.Now:yyyy-MM-dd}error.log"), log);
                }
            }
        }
    }
}