using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Pingfan.Kit;

namespace Pingfan.WebServer
{
    public class HttpRequest : IDisposable
    {
        private HttpListenerContext _HttpListenerContext = null;

        public HttpRequest()
        {
        }


        internal void SetListenerContext(HttpListenerContext httpContext)
        {
            _HttpListenerContext = httpContext;
        }


        public Uri Url
        {
            get => this._HttpListenerContext.Request.Url;
        }

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
                    var path = Regex.Replace(this.Url.LocalPath, "//", "/");
                    path = Regex.Replace(path, @"\\", @"\");
                    _LocalPath = path;
                }

                return _LocalPath;
            }
        }


        /// <summary>
        /// 浏览器请求的方法, 固定大写的
        /// </summary>

        public string Method
        {
            get => this._HttpListenerContext.Request.HttpMethod.ToUpper();
        }

        /// <summary>
        /// 请求提交来的数据流
        /// </summary>
        public Stream InputStream => _HttpListenerContext.Request.InputStream;

        /// <summary>
        /// 请求头
        /// </summary>
        public NameValueCollection Headers
        {
            get => _HttpListenerContext.Request.Headers;
        }

        /// <summary>
        /// 客户端浏览器类型
        /// </summary>
        public string UserAgent => _HttpListenerContext.Request.UserAgent;

        private string _SystemName;

        /// <summary>
        /// 客户端设备类型
        /// </summary>
        public string Device
        {
            get
            {
                if (string.IsNullOrEmpty(_SystemName))
                {
                    _SystemName = UserAgent;
                    if (!string.IsNullOrEmpty(_SystemName))
                    {
                        if (_SystemName.ContainsIgnoreCase("Android"))
                        {
                            _SystemName = "Android";
                        }
                        else if (_SystemName.ContainsIgnoreCase("iPhone"))
                        {
                            _SystemName = "iPhone";
                        }
                        else if (_SystemName.ContainsIgnoreCase("iPad"))
                        {
                            _SystemName = "iPad";
                        }
                        else if (_SystemName.ContainsIgnoreCase("Windows Phone"))
                        {
                            _SystemName = "Windows Phone";
                        }
                        else if (_SystemName.ContainsIgnoreCase("Windows NT"))
                        {
                            _SystemName = "Windows";
                        }
                        else if (_SystemName.ContainsIgnoreCase("Mac OS"))
                        {
                            _SystemName = "Mac OS";
                        }
                        else if (_SystemName.ContainsIgnoreCase("Linux"))
                        {
                            _SystemName = "Linux";
                        }
                        else
                        {
                            _SystemName = "Other";
                        }
                    }
                }

                return _SystemName;
            }
        }

        /// <summary>
        /// 获取提交的账号密码
        /// </summary>
        /// <returns></returns>
        public void GetAuth(out string userName, out string password)
        {
            var authorization = Headers["Authorization"];
            if (string.IsNullOrEmpty(authorization))
            {
                userName = null;
                password = null;
                return;
            }

            if (authorization.StartsWith("Basic "))
            {
                authorization = authorization.Substring(6, authorization.Length - 6);
            }

            try
            {
                var auth = Encoding.UTF8.GetString(Convert.FromBase64String(authorization));
                var t = auth.Split(':');
                if (t.Length == 2)
                {
                    userName = t[0];
                    password = t[1];
                    return;
                }
            }
            catch (Exception e)
            {
            }

            userName = null;
            password = null;
            return;
        }

        private string _QueryString;

        /// <summary>
        /// 以字符串形式获取GET的数据, 同时支持回写, 可做加解密类操作
        /// </summary>
        public string QueryString
        {
            get
            {
                if (string.IsNullOrEmpty(_QueryString))
                {
                    _QueryString = this._HttpListenerContext.Request.Url.Query;
                }

                return _QueryString;
            }
            set => _QueryString = value;
        }


        private string _PostString;

        /// <summary>
        /// 以字符串形式获取POST的数据, 同时支持回写, 可做加解密类操作
        /// </summary>
        public string PostString
        {
            get
            {
                if (string.IsNullOrEmpty(_PostString))
                {
                    var sr = new StreamReader(InputStream);
                    _PostString = sr.ReadToEnd();
                }

                return _PostString;
            }
            set => _PostString = value;
        }

        /// <summary>
        /// 获取key=value形式GET中的数据
        /// </summary>
        public string Get(string key, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(QueryString))
                return defaultValue;


            //获取到的值
            string value;
            var m = Regex.Match(QueryString, key + "=([^&]+)");
            if (m.Success)
            {
                value = m.Groups[1].Value;
                return value;
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 获取key=value形式GET中的数据, 同时转换好类型
        /// </summary>
        public T GetKV<T>(string key, T defaultValue = default(T))
        {
            var data = GetKV<T>(key);
            try
            {
                return (T) Convert.ChangeType(data, typeof(T));
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }


        /// <summary>
        /// 获取key=value形式POST中的数据
        /// </summary>
        public string PostKV(string key, string defaultValue = "")
        {
            if (string.IsNullOrEmpty(PostString))
                return defaultValue;

            //获取到的值
            string value;
            var m = Regex.Match(PostString, key + "=([^&]+)");
            if (m.Success)
            {
                value = m.Groups[1].Value;
                return value;
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 获取key=value形式POST中的数据, 同时转换好类型
        /// </summary>
        /// <param name="key"></param>
        /// <param name="dValue"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T PostKV<T>(string key, T defaultValue = default(T))
        {
            var data = PostKV(key);
            try
            {
                return (T) ConvertEx.ChangeType(data, typeof(T));
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

#if NETCOREAPP

        private System.Text.Json.JsonDocument _PostJson;
        public System.Text.Json.JsonDocument PostJson
        {
            get
            {
                if (_PostJson == null && PostString.IsNullOrWhiteSpace() == false)
                {
                    _PostJson = System.Text.Json.JsonDocument.Parse(PostString);
                }

                return _PostJson;
            }
        }


        /// <summary>
        /// 从POST中获取JSON的根节点中的数据
        /// </summary>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public string PostJsonKey(string name, string defaultValue = default)
        {
            return PostJsonKey<string>(name);
        }

        /// <summary>
        /// 从POST中获取JSON的根节点中的数据
        /// </summary>
        /// <param name="name">JSON的key, 区分大小写</param>
        /// <param name="defaultValue"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T PostJsonKey<T>(string name, T defaultValue = default(T))
        {
            if (PostJson == null || PostJson.RootElement.TryGetProperty(name, out var json) == false)
            {
                return defaultValue;
            }

            return (T) ConvertEx.ChangeType(json.ToString(), typeof(T));
        }
#endif

        /// <summary>
        /// 获取请求的Cookie
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetCookie(string key)
        {
            var cookie = _HttpListenerContext.Request.Cookies[key];

            if (cookie != null)
            {
                return cookie.Value;
            }

            return string.Empty;
        }


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
                    string[] ipHeads = {"CF-Connecting-IP", "X_FORWARDED_FOR", "X-Forwarded-For", "X-Real-IP"};
                    var ips = new List<string>();
                    foreach (var head in ipHeads)
                    {
                        var t = _HttpListenerContext.Request.Headers[head];
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
                            t = ip.Split(new char[] {',', ':', '[', ']'}, StringSplitOptions.RemoveEmptyEntries)[0];
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
            InputStream?.Dispose();
        }
    }
}