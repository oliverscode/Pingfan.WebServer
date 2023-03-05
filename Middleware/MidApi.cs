using Pingfan.Kit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;


namespace Pingfan.WebServer.Middleware
{
    /// <summary>
    /// API的中间件
    /// </summary>
    public class MidApi : IMiddleware
    {
        /// <summary>
        /// 控制器列表
        /// </summary>
        private Dictionary<string, MidApiItem> _Controllers =
            new Dictionary<string, MidApiItem>(StringComparer.OrdinalIgnoreCase);

        public MidApi()
        {
        }

        /// <summary>
        /// 可默认配置一些参数
        /// </summary>
        /// <param name="fn"></param>
        public MidApi(Action<MidApi> fn)
        {
            fn(this);
        }

        public void Add<THttpContext>() where THttpContext : HttpContext
        {
            Add<THttpContext>(null);
        }

        /// <summary>
        /// 添加一组控制器
        /// </summary>
        /// <typeparam name="THttpContext">必须是HttpContext的子类</typeparam>
        /// <exception cref="Exception"></exception>
        public void Add<THttpContext>(string urlPrefix) where THttpContext : HttpContext
        {
            var type = typeof(THttpContext);
            var mds = type.GetMethods();

            // 判断是否是HttpContextBase的子类
            if (type.IsSubclassOf(typeof(HttpContext)) == false)
            {
                throw new Exception($"{type.Name}必须是HttpContext的子类");
            }

            // 构造函数
            var SetHttpContext =
                type.GetRuntimeMethods().FirstOrDefault(p => p.Name == "SetHttpContext");


            foreach (var methodInfo in mds)
            {
                // 不是公开的, 不是静态的
                if (methodInfo.DeclaringType.Name != type.Name
                    || methodInfo.IsPublic == false
                    || methodInfo.IsStatic
                   )
                    continue;

                var isAsync = methodInfo.IsDefined(typeof(AsyncStateMachineAttribute), false);
                if (isAsync)
                {
                    // 判断返回值是否是Task
                    if (methodInfo.ReturnType != typeof(Task))
                        throw new Exception($"{type.Name}->{methodInfo.Name}异步方法必须返回Task");
                }

                _Controllers.Add(
                    string.IsNullOrWhiteSpace(urlPrefix)
                        ? $"/{type.Name}/{methodInfo.Name}"
                        : $"/{urlPrefix}/{type.Name}/{methodInfo.Name}", new MidApiItem()
                        {
                            Method = methodInfo,
                            Context = type,
                            SetHttpContext = SetHttpContext,
                        });
            }
        }


        public void Invoke(HttpContext ctx, Action<HttpContext> next)
        {
            var url = ctx.Request.LocalPath;

            // 不存在的控制器
            if (_Controllers.ContainsKey(url) == false)
            {
                next(ctx);
                return;
            }

            var controller = _Controllers[url];
            var page = (HttpContext)ExpressionEx.CreateInstance(controller.Context);


            // 创建新的类型
            controller.SetHttpContext.Invoke(page, new object[] { ctx });

            // 解析参数
            var ps = controller.Method.GetParameters();
            var args = new object[ps.Length];
            for (var i = 0; i < ps.Length; i++)
            {
                var p = ps[i];
                try
                {
                    // 直接从POST中获取
                    var v = ctx.Request.PostKV(p.Name);
                    if (v.IsNullOrWhiteSpace() == false)
                    {
                        // 把v转换类型
                        args[i] = ConvertEx.ChangeType(v, p.ParameterType);
                        continue;
                    }

                    // 从POST JSON中获取
                    v = ctx.Request.PostJsonKey(p.Name);
                    if (v.IsNullOrWhiteSpace() == false)
                    {
                        args[i] = ConvertEx.ChangeType(v, p.ParameterType);
                        continue;
                    }

                    // 从GET中获取
                    v = ctx.Request.Get(p.Name);
                    if (v.IsNullOrWhiteSpace() == false)
                    {
                        args[i] = ConvertEx.ChangeType(v, p.ParameterType);
                        continue;
                    }

                    // 判断是否有默认值
                    if (p.HasDefaultValue)
                    {
                        args[i] = p.DefaultValue;
                        continue;
                    }

                    // 判断p是否允许为空
                    if (p.ParameterType.IsClass)
                    {
                        args[i] = null;
                        continue;
                    }

                    // 判断是否继承自Nullable
                    if (p.ParameterType.IsGenericType
                        && p.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        args[i] = null;
                        continue;
                    }
                }
                catch (Exception e)
                {
                    throw new ArgumentException($"{p.Name}参数不正确", e);
                }

                // 缺少参数, 抛出异常
                throw new Exception($"缺少参数{p.Name}");
            }

            // 执行api主体, 支持异步
            var result = controller.Method.Invoke(page, args) as Task;
            result?.Wait();

            // api返回的内容写到原来的流
            page.Response.OutputStream.WriteTo(ctx.Response.OutputStream);

            page.Dispose();
            next(ctx);
        }


        class MidApiItem
        {
            /// <summary>
            /// 方法主体
            /// </summary>
            public MethodInfo Method { get; set; }

            public Type Context { get; set; }
            public MethodInfo SetHttpContext { get; set; }
        }


    }
}