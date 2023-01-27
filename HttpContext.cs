using System;
using System.Net;

namespace Pingfan.WebServer
{
    // public class HttpContextBase : HttpContextBase<HttpRequestBase, HttpResponseBase>
    // {
    // }

    public class HttpContext : IDisposable
    {
        internal HttpListenerContext _HttpListenerContext;

        /// <summary>
        /// 第几个中间件时执行的
        /// </summary>
        internal int MidIndex { get; set; }


        /// <summary>
        /// 设置内置HttpListenerContext对象
        /// </summary>
        /// <param name="httpContext"></param>
        internal void SetHttpListenerContext(HttpListenerContext httpContext)
        {
            this._HttpListenerContext = httpContext;
            this.OnStart();
        }
        
        
        internal void SetHttpContext(HttpContext httpContext)
        {
            this.MidIndex = httpContext.MidIndex;
            this._HttpListenerContext = httpContext._HttpListenerContext;
            this.OnStart();
        }

        /// <summary>
        /// 页面准备就绪后
        /// </summary>
        public virtual void OnStart()
        {
            
        }
        

        private HttpRequest _Request;

        /// <summary>
        /// 请求的对象
        /// </summary>
        public HttpRequest Request
        {
            get
            {
                if (_Request == null)
                {
                    _Request = new HttpRequest();
                    _Request.SetListenerContext(this._HttpListenerContext);
                }

                return _Request;
            }
        }

        private HttpResponse _Response;

        /// <summary>
        /// 响应的对象
        /// </summary>
        public HttpResponse Response
        {
            get
            {
                if (_Response == null)
                {
                    _Response = new HttpResponse();
                    _Response.SetListenerContext(this._HttpListenerContext);
                }

                return _Response;
            }
        }

        public void Dispose()
        {
            this.Request.Dispose();
            this.Response.Dispose();
        }
    }
}