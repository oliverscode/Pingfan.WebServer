using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Pingfan.Kit;


namespace Pingfan.WebServer.Middleware
{
    public class MidStaticFile : IMiddleware
    {
        public MidStaticFile()
        {
        }

        /// <summary>
        /// 静态文件目录
        /// </summary>
        private HashSet<string> _WWWRoot { get; set; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 添加一个静态文件映射目录
        /// </summary>
        /// <param name="dir">本地绝对目录</param>
        public bool AddDirectory(string dir)
        {
           
            if (dir.EndsWith(Path.DirectorySeparatorChar.ToString()) == false)
            {
                dir += Path.DirectorySeparatorChar;
            }

            // 目录不存在就不添加了
            if (Directory.Exists(dir) == false)
                return false;
            _WWWRoot.Add(dir);
            return true;
        }

        /// <summary>
        /// 默认首页
        /// </summary>
        private HashSet<string> _DefaultFiles { get; set; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "index.html",
                "default.html",
            };

        /// <summary>
        /// 添加一个默认首页文件
        /// </summary>
        /// <param name="file"></param>
        public void AddDefaultFile(string file)
        {
            _DefaultFiles.Add(file);
        }


        /// <summary>
        /// 可默认配置一些参数
        /// </summary>
        /// <param name="fn"></param>
        public MidStaticFile(Action<MidStaticFile> fn)
        {
            fn(this);
        }

        public void Invoke(HttpContext ctx, Action<HttpContext> next)
        {
            var fileName = ctx.Request.LocalPath.EndsWith("/") ? "" : ctx.Request.LocalPath;

            // 判断本地是否存在这个文件
            foreach (var path in _WWWRoot)
            {
                if (fileName.IsNullOrWhiteSpace())
                {
                    foreach (var defaultFile in _DefaultFiles)
                    {
                        var localPath = PathEx.Combine(path, ctx.Request.LocalPath, defaultFile);

                        // 存在就不继续了
                        if (File.Exists(localPath))
                        {
                            ctx.Response.WriteFile(localPath);
                            return;
                        }
                    }
                }
                else
                {
                    var localPath = PathEx.Combine(path, fileName);

                    // 存在就不继续了
                    if (File.Exists(localPath))
                    {
                        ctx.Response.WriteFile(localPath);
                        return;
                    }
                }
            }

            // 找不到静态文件, 执行后续
            next(ctx);
        }
    }
}