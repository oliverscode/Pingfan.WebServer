using System;
using System.Collections.Generic;

namespace Pingfan.WebServer.WebSockets
{
    public interface IWebSocketHandler
    {
        /// <summary>
        /// 客户端已经连接上后
        /// </summary>
        /// <param name="client">当前客户端</param>
        public void OnOpened(WebSocketContext client);


        /// <summary>
        /// 收到客户端发来的数据
        /// </summary>
        /// <param name="client">当前客户端</param>
        /// <param name="buffer"></param>
        public void OnReceived(WebSocketContext client, byte[] buffer);


        /// <summary>
        /// 客户端已经离线
        /// </summary>
        /// <param name="client">当前客户端</param>
        public void OnClosed(WebSocketContext client);

        /// <summary>
        /// 业务发生错误时
        /// </summary>
        /// <param name="client">当前客户端</param>
        /// <param name="ex"></param>
        public void OnError(WebSocketContext client, Exception ex);
    }
}