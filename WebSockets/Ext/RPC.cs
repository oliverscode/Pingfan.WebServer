// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Reflection;
// using System.Text.Json;
// using System.Threading.Tasks;
// using Pingfan.Kit;
//
// namespace PingFan.WebServer.WebSockets.Ext
// {
//     public class RPC : WebSocketHandler<WebSocketContext>
//     {
//         private readonly Dictionary<string, MethodInfo> _Methods
//             = new Dictionary<string, MethodInfo>(StringComparer.OrdinalIgnoreCase);
//
//         /// <summary>
//         /// 添加全部静态方法
//         /// </summary>
//         /// <typeparam name="T"></typeparam>
//         public void AddMethods<T>() where T : new()
//         {
//             var type = typeof(T);
//             var methods = type.GetMethods();
//             foreach (var method in methods)
//             {
//                 // 不是公开的, 是静态的
//                 if (method.DeclaringType.Name != type.Name
//                     || method.IsPublic == false
//                     || method.IsStatic == false
//                    )
//                     continue;
//
//                 var parameters = method.GetParameters();
//                 if (parameters.Length == 3
//                     && parameters[0].ParameterType == typeof(List<TWebSocket>)
//                     && parameters[1].ParameterType == typeof(TWebSocket)
//                     && parameters[2].ParameterType == typeof(JsonElement)
//                    )
//                 {
//                     _Methods.Add(method.Name, method);
//                 }
//             }
//         }
//
//
//         public override void OnWebSocketReceived(List<TWebSocket> onlines, TWebSocket client, string msg)
//         {
//             var item = JsonSerializer.Deserialize<RPCItem>(msg);
//             if (item.Action.EqualsIgnoreCase("HeartBeat"))
//             {
//                 // 获取毫秒级时间戳
//                 var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
//
//                 // 客户端发送的时间, 只记录, 不做处理
//                 var json = (JsonElement)item.Args;
//                 var sendTimestamp = json.GetProperty("ClientLastSendTimestamp").GetInt64();
//                 client.ClientLastSendTimestamp = sendTimestamp;
//                 client.ServerLastReceiveTimestamp = timestamp;
//                 
//                 return;
//             }
//
//             if (item.Broadcast)
//             {
//              
//                 foreach (var webSocketContext in onlines)
//                 {
//                     try
//                     {
//                         webSocketContext.Send(msg);
//                     }
//                     catch (Exception)
//                     {
//                        
//                     }
//                     
//                 }
//             }
//
//             if (_Methods.ContainsKey(item.Action))
//             {
//                 var fn = _Methods[item.Action];
//                 var result = fn?.Invoke(null, new Object[] { onlines, client, item.Args }) as Task;
//                 if (result != null)
//                 {
//                     result.Wait();
//                 }
//             }
//         }
//
//         public override void OnWebSocketError(List<TWebSocket> onlines, TWebSocket client, Exception ex)
//         {
//             Console.WriteLine("异常属于大错误了"+ex);
//         }
//     }
//     
//     class RPCItem
//     {
//         public string Action { get; set; }
//         public object Args { get; set; }
//         
//         /// <summary>
//         /// 是否广播
//         /// </summary>
//         public bool Broadcast { get; set; }
//         
//      
//     }
// }