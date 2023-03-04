using System;
using System.IO;
using System.Net;
using System.Text;

using Pingfan.WebServer.Tools;

namespace Pingfan.WebServer
{
    public class HttpResponse : IDisposable
    {


        private HttpListenerContext _HttpListenerContext = null;

        public HttpResponse()
        {
        }


        internal void SetListenerContext(HttpListenerContext httpContext)
        {
            _HttpListenerContext = httpContext;
        }

        /// <summary>
        /// 响应头
        /// </summary>
        public WebHeaderCollection Headers
        {
            get => _HttpListenerContext.Response.Headers;
        }

        /// <summary>
        /// 是否以分块的形式发送到客户端
        /// </summary>
        public bool SendChunked
        {
            get => _HttpListenerContext.Response.SendChunked;
            set => _HttpListenerContext.Response.SendChunked = value;
        }

        /// <summary>
        /// 是否保持长连接, 默认保持
        /// </summary>
        public bool KeepAlive
        {
            get => _HttpListenerContext.Response.KeepAlive;
            set => _HttpListenerContext.Response.KeepAlive = value;
        }

        private MemoryStream _outputStream;

        /// <summary>
        /// 本地输出内存流
        /// </summary>
        public MemoryStream OutputStream
        {
            get
            {
                if (_outputStream == null)
                    _outputStream = new MemoryStream();
                return _outputStream;
            }
        }

        /// <summary>
        /// 响应客户端的字符串内容
        /// </summary>
        public string BodyString
        {
            get
            {
                if (OutputStream.Length < 2048)
                {
                    // 读取OutputStream的内容, 同时不影响后续的写入
                    OutputStream.Position = 0;
                    var sr = new StreamReader(OutputStream);
                    return sr.ReadToEnd();
                }
                else
                {
                    // 读取OutputStream的内容, 同时不影响后续的写入
                    OutputStream.Position = 0;
                    var sr = new StreamReader(OutputStream);

                    // 读取sr中的前后20个字符
                    var front = new char[20];
                    sr.BaseStream.Position = 0;
                    sr.Read(front, 0, 20);

                    var back = new char[20];
                    sr.BaseStream.Position = sr.BaseStream.Length - 20;
                    sr.Read(back, 0, 20);

                    //front和end连接起来
                    var sb = new StringBuilder();
                    sb.Append(front);
                    sb.Append("...");
                    sb.Append(back);
                    return sb.ToString();
                }
            }
            set
            {
                OutputStream.SetLength(0);
                using (var sw = new StreamWriter(OutputStream))
                {
                    sw.Write(value);
                }


            }
        }

        /// <summary>
        /// 设置输出内容编码, 默认UTF8
        /// </summary>
        public Encoding ContentEncoding
        {
            get
            {
                if (_HttpListenerContext.Response.ContentEncoding == null)
                {
                    _HttpListenerContext.Response.ContentEncoding = Encoding.UTF8;
                }

                return _HttpListenerContext.Response.ContentEncoding;
            }
            set => _HttpListenerContext.Response.ContentEncoding = value;
        }

        /// <summary>
        /// 状态码
        /// </summary>
        public int StatusCode
        {
            get => _HttpListenerContext.Response.StatusCode;
            set => _HttpListenerContext.Response.StatusCode = value;
        }


        /// <summary>
        /// 返回文档类型
        /// </summary>
        public string ContentType
        {
            get => _HttpListenerContext.Response.ContentType;
            set => _HttpListenerContext.Response.ContentType = value;
        }


        /// <summary>
        /// 开启权限验证模式
        /// </summary>
        public void StartAuth()
        {
            //WWW-Authenticate: Basic realm="Secure Area"
            StatusCode = 401;
            Headers["WWW-Authenticate"] = "Basic ";
        }

        /// <summary>
        /// 重定向到一个新地址
        /// </summary>
        /// <param name="url"></param>
        public void Redirect(string url)
        {
            StatusCode = 302;
            // Headers.Add("Location", url);
            this._HttpListenerContext.Response.Redirect(url);
            this.End();
        }

        /// <summary>
        /// 写入Cookie
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetCookie(string key, string value)
        {
            this.SetCookie(key, value, DateTime.Now.AddYears(10));
        }

        /// <summary>
        /// 写入Cookie
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expires"></param>
        public void SetCookie(string key, string value, DateTime expires)
        {
            var cookie = new Cookie(key, value);
            cookie.Path = "/";
            cookie.Expires = expires;
            cookie.Domain = _HttpListenerContext.Request.Url.Host;
            this._HttpListenerContext.Response.SetCookie(cookie);
        }



        /// <summary>
        /// 写入一个字符串
        /// </summary>
        /// <param name="text"></param>
        public void Write(string text)
        {
            var result = ContentEncoding.GetBytes(text);
            Write(result);
        }

        /// <summary>
        /// 写入一个字节数组
        /// </summary>
        /// <param name="buffer"></param>
        public void Write(byte[] buffer)
        {
            if (OutputStream.CanWrite)
                OutputStream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// 写入一个文件
        /// </summary>
        /// <param name="path"></param>
        public void WriteFile(string path)
        {
            this.ContentType = Tools.HttpMime.Get(Path.GetExtension(path));
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.CopyTo(this.OutputStream);
            }
        }

        /// <summary>
        /// 下载一个文件
        /// </summary>
        /// <param name="path"></param>
        public void DownloadFile(string path)
        {
            this.ContentType = Tools.HttpMime.Get(Path.GetExtension(path));
            this.Headers.Set("Content-Disposition", "attachment; filename=" + Path.GetFileName(path));
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.CopyTo(this.OutputStream);
            }
        }
#if NETCOREAPP
        /// <summary>
        /// 写入一个JSON对象
        /// </summary>
        /// <param name="json"></param>
        public void Json(object json)
        {
            this.ContentType = HttpMime.Get(".json");
            this.Write(System.Text.Json.JsonSerializer.Serialize(json, JsonSerializerOptions));
        }


        public static System.Text.Json.JsonSerializerOptions JsonSerializerOptions = new System.Text.Json.JsonSerializerOptions()
        {
            // 中文支持
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),

            // 忽略空值
            IgnoreNullValues = true,

            // 全部大写参照上面注释代码
            // PropertyNamingPolicy = new UpperCaseNamingPolicy(),
        };

#endif

        /// <summary>
        /// 清空所有输出缓存
        /// </summary>
        public void Clear()
        {
            OutputStream.SetLength(0);
        }

        /// <summary>
        /// 立即结束执行, 并返回所有输出
        /// </summary>
        public void End()
        {
            throw new HttpEndException();
        }

        public void Dispose()
        {
            _outputStream?.Dispose();
        }
    }
}