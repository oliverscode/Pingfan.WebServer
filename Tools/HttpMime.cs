using Pingfan.Kit;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pingfan.WebServer.Tools
{
    public class HttpMime
    {
        private static readonly Dictionary<string, string> Mimes =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {".html", "text/html; charset=UTF-8"},
                {".css", "text/css"},
                {".xml", "text/xml"},

                {".gif", "image/gif"},
                {".jpeg", "image/jpeg"},
                {".jpg", "image/jpeg"},
                {".png", "image/png"},

                {".svg", "image/svg+xml"},
                {".svgz", "image/svg+xml"},
                {".tif", "image/tiff"},
                {".tiff", "image/tiff"},
                {".ico", "image/x-icon"},
                {".bmp", "image/x-ms-bmp"},


                {".woff", "font-woff"},
                {".woff2", "font-woff2"},


                {".mp3", "audio/mpeg"},
                {".ogg", "audio/ogg"},

                {".mp4", "video/mp4"},
                {".ts", "video/mp2t"},


                {".js", "application/javascript"},
                {".json", "application/json"},

                {".txt", "text/plain"},
            };

        public static string Get(string key)
        {
            if (Mimes.ContainsKey(key))
                return Mimes[key];

            var item = Mimes.FirstOrDefault(p => p.Key.ContainsIgnoreCase(key)   );
            if (item.Value != null)
                return item.Value;
            return "application/octet-stream";
        }

        public static void Set(string key, string value)
        {
            if (Mimes.ContainsKey(key) == false)
                Mimes[key] = value;
        }
    }
}