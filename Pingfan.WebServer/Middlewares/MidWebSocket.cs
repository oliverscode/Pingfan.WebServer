using System.Net.WebSockets;
using System.Text;
using Pingfan.Inject;
using Pingfan.WebServer.Interfaces;

namespace Pingfan.WebServer.Middlewares;

public class MidWebSocket : IMiddleware
{
    public Encoding Encoding { get; set; } = Encoding.UTF8;

    /// <summary>
    /// 检查请求是否合法
    /// </summary>
    public event Func<IHttpContext, bool> Check = null!;

    /// <summary>
    /// 客户端已经连接上后
    /// </summary>
    // public void OnOpen(WebSocketContext context);
    public event Action<Websockets.WebSocketContext>? Open;

    /// <summary>
    /// 客户端关闭后
    /// </summary>
    public event Action<Websockets.WebSocketContext>? Close;

    // /// <summary>
    // /// 收到数据
    // /// </summary>
    public event Action<Websockets.WebSocketContext, byte[]>? Binary;

    //
    // /// <summary>
    // /// 收到文本数据
    // /// </summary>
    public event Action<Websockets.WebSocketContext, string>? Message;

    
    public async void Invoke(IContainer container, IHttpContext ctx, Action next)
    {
        if (ctx.Request.HttpListenerContext.Request.IsWebSocketRequest == false)
        {
            next();
            return;
        }


        if (Check.Invoke(ctx) == false)
        {
            next();
            return;
        }

        var subProtocol = ctx.Request.HttpListenerContext.Request.Headers["Sec-WebSocket-Protocol"];
        var listenerWebSocketContext = await ctx.Request.HttpListenerContext.AcceptWebSocketAsync(subProtocol);

        container.Push<HttpListenerWebSocketContext>(listenerWebSocketContext);
        var webSocketContext = container.New<Websockets.WebSocketContext>();

        Open?.Invoke(webSocketContext);
        // 接收数据
        while (webSocketContext.WebSocket.State == WebSocketState.Open)
        {
            var buffer = new byte[1024 * 4];
            var result = await webSocketContext.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer),
                CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                Close?.Invoke(webSocketContext);
                await webSocketContext.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty,
                    CancellationToken.None);
                break;
            }

            if (result.MessageType == WebSocketMessageType.Binary)
            {
                Binary?.Invoke(webSocketContext, buffer);
            }
            else if (result.MessageType == WebSocketMessageType.Text)
            {
                var msg = Encoding.GetString(buffer);
                Message?.Invoke(webSocketContext, msg);
            }
        }
    }
}