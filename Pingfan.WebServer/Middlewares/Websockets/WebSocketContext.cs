using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Pingfan.Inject;
using Pingfan.WebServer.Interfaces;

namespace Pingfan.WebServer.Middlewares.Websockets;

public class WebSocketContext
{
    private readonly IHttpResponse _httpResponse;
    public readonly HttpListenerWebSocketContext HttpListenerWebSocketContext;
    public IContainer? Container;
    public Encoding Encoding { get; set; } = Encoding.UTF8;
    public WebSocket WebSocket => this.HttpListenerWebSocketContext.WebSocket;
    public bool IsAvailable => this.HttpListenerWebSocketContext.WebSocket.State == WebSocketState.Open;

    public WebSocketContext(HttpListenerWebSocketContext httpListenerWebSocketContext, IHttpResponse httpResponse)
    {
        HttpListenerWebSocketContext = httpListenerWebSocketContext;
        _httpResponse = httpResponse;
    }

    public void Send(object json)
    {
        if (this.IsAvailable == false) return;
        var txt = JsonSerializer.Serialize(json, _httpResponse.JsonSerializerOptions);
        Send(txt);
    }

    public void Send(string message)
    {
        if (this.IsAvailable == false) return;
        var data = Encoding.GetBytes(message);
        Send(data);
        this.HttpListenerWebSocketContext.WebSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text,
            true, CancellationToken.None);
    }


    public void Send(byte[] data)
    {
        if (this.IsAvailable == false) return;
        this.HttpListenerWebSocketContext.WebSocket.SendAsync(new ArraySegment<byte>(data),
            WebSocketMessageType.Binary,
            true, CancellationToken.None);
    }

    public void Close()
    {
        if (this.IsAvailable == false) return;
        this.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None);
    }
}