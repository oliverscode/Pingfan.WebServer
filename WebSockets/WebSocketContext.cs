using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Timer = Pingfan.Kit.Timer;

namespace Pingfan.WebServer.WebSockets
{
    /// <summary>
    /// WebSocket对象
    /// </summary>
    public class WebSocketContext : IDisposable
    {
 
        internal HttpListenerContext _HttpListenerContext;
        internal System.Net.WebSockets.WebSocketContext _WebSocketContext;

        

        internal void SetHttpListenerContext(HttpListenerContext httpListenerContext,
            System.Net.WebSockets.WebSocketContext webSocketContext)
        {
            _WebSocketContext = webSocketContext;
            _HttpListenerContext = httpListenerContext;
            
        }
        
        /// <summary>
        /// 是否连接成功
        /// </summary>
        public bool IsConnected => _WebSocketContext.WebSocket.State == WebSocketState.Open;


        /// <summary>
        /// 发送字符串
        /// </summary>
        /// <param name="msg"></param>
        public void Send(string msg)
        {
            this.WebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(msg)), WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }

        /// <summary>
        /// 发送一个JSON对象
        /// </summary>
        /// <param name="data"></param>
        public void Send(object data)
        {
            Send(JsonSerializer.Serialize(data, HttpResponse._JsonSerializerOptions));
        }



        /// <summary>
        /// 从Http请求头里获取Cookie
        /// </summary>
        /// <param name="key">不区分大小写</param>
        /// <returns></returns>
        public string GetCookie(string key)
        {
            var cookie = this._WebSocketContext.CookieCollection[key];
            return cookie?.Value;
        }

        /// <summary>
        /// 获取Http请求头
        /// </summary>
        /// <param name="key">不区分大小写</param>
        /// <returns></returns>
        public string GetHeader(string key)
        {
            return this._WebSocketContext.Headers[key];
        }

        public Uri Uri => this._WebSocketContext.RequestUri;

        private string _LocalPath;

        /// <summary>
        /// 忽略了//和\的情况的URL
        /// </summary>
        public string LocalPath
        {
            get
            {
                if (string.IsNullOrEmpty(_LocalPath))
                {
                    var path = Regex.Replace(_WebSocketContext.RequestUri.LocalPath, "//", "/");
                    path = Regex.Replace(path, @"\\", @"\");
                    _LocalPath = path;
                }

                return _LocalPath;
            }
        }

        /// <summary>
        /// WebSocket收发对象
        /// </summary>
        private WebSocket WebSocket => this._WebSocketContext.WebSocket;


        private string _IP;


        /// <summary>
        /// 客户端请求的IP, 会尽可能获取真实IP, 支持CFCDN以及NGINX转发
        /// </summary>
        public string IP
        {
            get
            {
                if (string.IsNullOrEmpty(_IP))
                {
                    string[] ipHeads = { "CF-Connecting-IP", "X_FORWARDED_FOR", "X-Forwarded-For", "X-Real-IP" };
                    var ips = new List<string>();
                    foreach (var head in ipHeads)
                    {
                        var t = GetHeader(head);
                        if (string.IsNullOrEmpty(t) == false)
                        {
                            ips.Add(t);
                        }
                    }

                    ips.Add(_HttpListenerContext.Request.UserHostAddress);

                    // 只需要第一个IP
                    foreach (var ip in ips)
                    {
                        var t = "";
                        if (ip.Contains(",") || ip.Contains(":") || ip.Contains("["))
                        {
                            t = ip.Split(new char[] { ',', ':', '[', ']' }, StringSplitOptions.RemoveEmptyEntries)[0];
                            t = t.Trim();
                        }

                        // 判断t是ipv4地址
                        if (Regex.IsMatch(t, @"^(\d+)\.(\d+)\.(\d+)\.(\d+)$"))
                        {
                            _IP = t;
                            break;
                        }
                    }
                }

                return _IP;
            }
        }

        public void Dispose()
        {
            this.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "close", CancellationToken.None);
        }
        
    }
}