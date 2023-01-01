using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PingFan.WebServer.Tools;
using Pingfan.Kit;
using PingFan.WebServer.Middleware;
using PingFan.WebServer.WebSockets;
using WebSocketContext = PingFan.WebServer.WebSockets.WebSocketContext;

namespace PingFan.WebServer
{
    //字符串压缩
    //https://github.com/trullock/NUglify

    // public class WebServer : WebServer<HttpContext, WebSocketContext>
    // {
    // }
    //
    // public class WebServer<THttpContext> : WebServer<THttpContext, WebSocketContext>
    //     where THttpContext : HttpContext, new()
    // {
    // }

    public class WebServer
        //  where TWebSocketContext : WebSocketContext, new()
    {
        // 中间件列表
        private List<IMiddleware> _Middlewares = new List<IMiddleware>();

        private readonly HttpListener _HttpListener = new HttpListener();

        // HTTP路由列表
        private readonly ThreadSafeDictionary<string, Action<HttpContext>> _HttpTMaps =
            new ThreadSafeDictionary<string, Action<HttpContext>>(StringComparer.OrdinalIgnoreCase);

        // WebSocket处理列表
        internal ThreadSafeDictionary<string, IWebSocketHandler> _WebSocketHandlers =
            new ThreadSafeDictionary<string, IWebSocketHandler>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 默认的Http头中的Server字段
        /// </summary>
        public string DefaultServerName { get; set; } = "nginx";

        // WebSocket的在线列表
        private ThreadSafeList<WebSocketContext> _WebSocketOnlines =
            new ThreadSafeList<WebSocketContext>();

        private Type _HttpContextType = null;

        /// <summary>
        /// 默认HttpContext的构造类
        /// </summary>
        public Type HttpContextType
        {
            get
            {
                if (_HttpContextType == null)
                {
                    _HttpContextType = typeof(HttpContext);
                }

                return _HttpContextType;
            }
            set { _HttpContextType = value; }
        }

        // 是否是windows系统
        private static bool _IsWindows { get; set; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// 请求开始前执行
        /// </summary>
        public event Action<HttpContext> OnBeginRequest;

        /// <summary>
        /// 仅仅在OnBeginRequest之后执行
        /// </summary>
        public event Action<HttpContext> OnHttpHandler;


        /// <summary>
        /// 请求结束后执行
        /// </summary>
        public event Action<HttpContext> OnEndRequest;

        /// <summary>
        /// 有异常时执行
        /// </summary>
        public event Action<HttpContext, Exception> OnRequestError;


        /*
        /// <summary>
        /// WebSocket连接时执行
        /// </summary>
        internal event Action<List<TWebSocketContext>, TWebSocketContext> OnWebSocketOpened;

        /// <summary>
        /// WebSocket收到消息之前执行, 一般用于解密
        /// </summary>
        internal event Func<List<TWebSocketContext>, TWebSocketContext, string, string> OnWebSocketReceiveBefore;

        /// <summary>
        /// WebSocket收到消息后执行
        /// </summary>
        internal event Action<List<TWebSocketContext>, TWebSocketContext, string> OnWebSocketReceived;


        /// <summary>
        /// WebSocket向客户端发送之前执行, 一般用于加密
        /// </summary>
        internal event Func<List<TWebSocketContext>, TWebSocketContext, string, string> OnSendTextBefore;

        /// <summary>
        /// WebSocket关闭时执行
        /// </summary>
        internal event Action<List<TWebSocketContext>, TWebSocketContext> OnWebSocketClosed;

        /// <summary>
        /// WebSocket异常时执行
        /// </summary>
        internal event Action<List<TWebSocketContext>, TWebSocketContext, Exception> OnWebSocketError;
*/

        /// <summary>
        /// 开始监听端口
        /// </summary>
        /// <param name="port"></param>
        public void Listen(int port)
        {
            _HttpListener.Prefixes.Add("http://*:" + port + "/");
            _HttpListener.Start();

            // 循环执行
            Loop.RunWithTry(async () =>
            {
                var httpContext = await _HttpListener.GetContextAsync();
                // 异步执行
                Task.Run(async () =>
                {
                    // 根据系统清理头
                    if (_IsWindows)
                    {
                        httpContext.Response.Headers.Add("Server", "");
                    }
                    else
                    {
                        httpContext.Response.Headers.Add("Server", DefaultServerName);
                    }

                    if (httpContext.Request.IsWebSocketRequest)
                    {
                        // 支持客户端的WebSocket协议
                        var subProtocol = httpContext.Request.Headers["Sec-WebSocket-Protocol"];
                        var webSocketContext = await httpContext.AcceptWebSocketAsync(subProtocol);

                        // 执行websocket的逻辑
                        await ExecuteWebSocketContext(httpContext, webSocketContext);
                        return;
                    }

                    // 执行http的逻辑
                    ExecuteHttpContext(httpContext);
                });
            });
        }

        private async Task ExecuteWebSocketContext(HttpListenerContext httpListenerContext,
            System.Net.WebSockets.WebSocketContext webSocketContext)
        {
            if (webSocketContext.WebSocket.State != WebSocketState.Open)
            {
                return;
            }


            var websocketContext = new WebSocketContext();
            websocketContext.SetHttpListenerContext(httpListenerContext, webSocketContext);

            var localPath = websocketContext.LocalPath;
            if (_WebSocketHandlers.ContainsKey(localPath) == false)
            {
                httpListenerContext.Response.StatusCode = 404;
                await webSocketContext.WebSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "不支持的路径",
                    CancellationToken.None);
                return;
            }


            _WebSocketOnlines.Add(websocketContext);


            var handler = _WebSocketHandlers[localPath];
            try
            {
                handler.OnOpened(websocketContext);

                while (webSocketContext.WebSocket.State == WebSocketState.Open)
                {
                    // 缓冲
                    var buffer = new List<byte>();

                    while (webSocketContext.WebSocket.State == WebSocketState.Open)
                    {
                        // 每次读4K
                        var tempBuffer = new byte[1024 * 4];
                        var result =
                            await webSocketContext.WebSocket.ReceiveAsync(new ArraySegment<byte>(tempBuffer),
                                CancellationToken.None);

                        // 连接关闭
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            throw new WebSocketEndException();
                        }

                        if (result.Count <= 0)
                            break;

                        buffer.AddRange(tempBuffer.Take(result.Count));
                        if (result.EndOfMessage)
                            break;
                    }

                    handler.OnReceived(websocketContext, buffer.ToArray());
                }
            }
            catch (WebSocketEndException e)
            {
            }
            catch (Exception e)
            {
                handler.OnError(websocketContext, e);
            }
            finally
            {
                _WebSocketOnlines.Remove(websocketContext);
                await webSocketContext.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "close",
                    CancellationToken.None);
                handler.OnClosed(websocketContext);
            }
        }

        // 正式执行流程
        private void ExecuteHttpContext(HttpListenerContext http)
        {
            var httpContext = (HttpContext)ExpressionEx.CreateInstance(HttpContextType);
            try
            {
                httpContext.SetHttpListenerContext(http);
                http.Response.SendChunked = false;

                // 默认是html
                httpContext.Response.ContentType = HttpMime.Get(".html");

                // 如果http协议大于1.1 就启用SendChunked
                if (httpContext._HttpListenerContext.Request.ProtocolVersion >= HttpVersion.Version11)
                {
                    http.Response.SendChunked = true;
                    httpContext.Response.KeepAlive = true;
                }

                this.OnBeginRequest?.Invoke(httpContext);


                this.OnHttpHandler?.Invoke(httpContext);

                var localPath = httpContext.Request.LocalPath;
                var action = GetMap(localPath);

                if (action != null)
                {
                    action(httpContext);
                }


                // 开始执行中间件
                Next(httpContext);


                this.OnEndRequest?.Invoke(httpContext);

                // 如果是空的就是404
                if (httpContext.Response.OutputStream.Length <= 0)
                {
                    httpContext.Response.StatusCode = 404;
                }
            }
            catch (HttpEndException)
            {
            }
            catch (Exception ex)
            {
                try
                {
                    if (httpContext.Response.StatusCode < 500)
                        httpContext.Response.StatusCode = 500;
                    this.OnRequestError?.Invoke(httpContext, ex);
                }
                catch (Exception e)
                {
                }
            }
            finally
            {
                try
                {
                    if (httpContext.Response.OutputStream.Length > 0
                        && http.Response.OutputStream.CanWrite
                        // 只要头就不要主体了
                        && http.Request.HttpMethod.EqualsIgnoreCase("HEAD") == false)
                    {
                        http.Response.ContentLength64 = httpContext.Response.OutputStream.Length;
                        httpContext.Response.OutputStream.WriteTo(http.Response.OutputStream);
                    }
                }
                catch (Exception)
                {
                }

                http.Response.Close();
            }
        }

        /// <summary>
        /// 映射一个路由
        /// </summary>
        /// <param name="url"></param>
        /// <param name="fn"></param>
        public void Map(string url, Action<HttpContext> fn)
        {
            _HttpTMaps[url] = fn;
        }

        /// <summary>
        /// 映射一个类的所有公开静态方法为路由
        /// </summary>
        /// <param name="type"></param>
        public void Map(Type type)
        {
            var bindingAttr = BindingFlags.Static | BindingFlags.Public;
            var methods = type.GetMethods(bindingAttr);
            foreach (MethodInfo methodInfo in methods)
            {
                if (methodInfo.GetParameters().Length == 1)
                {
                    string url = "/" + type.Name + "/" + methodInfo.Name;

                    if (methodInfo.GetParameters()[0].ParameterType == typeof(HttpContext))
                    {
                        var fn = (Action<HttpContext>)methodInfo.CreateDelegate(typeof(Action<HttpContext>));
                        Map(url, fn);
                    }
                }
            }
        }

        /// <summary>
        /// 映射一个类的所有公开静态方法为路由, 但要自定义类名
        /// </summary>
        /// <param name="type"></param>
        public void Map(string controllerName, Type type)
        {
            var bindingAttr = BindingFlags.Static | BindingFlags.Public;
            var methods = type.GetMethods(bindingAttr);
            foreach (MethodInfo methodInfo in methods)
            {
                if (methodInfo.GetParameters().Length == 1)
                {
                    string url = "/" + controllerName + "/" + methodInfo.Name;
                    if (methodInfo.CreateDelegate(typeof(Action<HttpContext>)) is Action<HttpContext> fn)
                    {
                        Map(url, fn);
                    }
                }
            }
        }

        // 获取一个路由
        private Action<HttpContext> GetMap(string url)
        {
            if (_HttpTMaps.TryGetValue(url, out var fn))
            {
                return fn;
            }

            return null;
        }

        /// <summary>
        /// 添加一个中间件
        /// </summary>
        /// <param name="middleware"></param>
        public void Use(IMiddleware middleware)
        {
            _Middlewares.Add(middleware);
        }

        /// <summary>
        /// 引入一个WebSocket处理器
        /// </summary>
        public bool UseWebSocket(string url, IWebSocketHandler handler)
        {
            return _WebSocketHandlers.TryAdd(url, handler);
        }

        // 执行下一个中间件
        private void Next(HttpContext context)
        {
            var nextIndex = context.MidIndex;
            if (nextIndex >= _Middlewares.Count)
                return;
            var mid = _Middlewares[nextIndex];
            context.MidIndex += 1;
            mid.Invoke(context, Next);
        }
    }
}