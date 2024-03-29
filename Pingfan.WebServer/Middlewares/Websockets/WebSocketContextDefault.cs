﻿using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Pingfan.WebServer.Interfaces;

namespace Pingfan.WebServer.Middlewares.Websockets;

public class WebSocketContextDefault : IWebSocketContext
{
    private readonly IHttpResponse _httpResponse;
    public Encoding Encoding { get; set; }
    public string Protocol => this.HttpListenerWebSocketContext.WebSocket.SubProtocol!;
    public readonly HttpListenerWebSocketContext HttpListenerWebSocketContext;

    public WebSocket WebSocket => this.HttpListenerWebSocketContext.WebSocket;
    public bool IsAvailable => this.HttpListenerWebSocketContext.WebSocket.State == WebSocketState.Open;

    public WebSocketContextDefault(
        HttpListenerWebSocketContext httpListenerWebSocketContext,
        IHttpResponse httpResponse,
        Encoding encoding)
    {
        HttpListenerWebSocketContext = httpListenerWebSocketContext;
        _httpResponse = httpResponse;
        Encoding = encoding;
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
        this.HttpListenerWebSocketContext.WebSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text,
            true, CancellationToken.None);
    }


    public void Send(byte[] data)
    {
        if (this.IsAvailable == false) return;
        this.HttpListenerWebSocketContext.WebSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary,
            true, CancellationToken.None);
    }

    public void Close()
    {
        if (this.IsAvailable == false) return;
        this.WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None);
    }

    public virtual bool OnCheck()
    {
        return false;
    }

    public virtual void OnOpen()
    {
    }

    public virtual void OnClose()
    {
    }

    public virtual void OnBinary(byte[] data)
    {
    }

    public virtual void OnMessage(string message)
    {
    }
}