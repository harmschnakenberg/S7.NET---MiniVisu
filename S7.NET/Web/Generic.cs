using Grapevine;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace S7.NET.web
{
    class Html
    {
        private static readonly string appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        public static string Page(string fileName, Dictionary<string, string> insert)
        {
            string path = Path.Combine(appPath, "Templates", fileName);

            if (!File.Exists(path))
                return "<p>Datei nicht gefunden: <i>" + path + "</i><p>";

            string template = System.IO.File.ReadAllText(path);

            StringBuilder sb = new StringBuilder(template);

            if (insert != null)
            {
                foreach (var key in insert.Keys)
                {
                    sb.Replace(key, insert[key]);
                }
            }

            return sb.ToString();
        }

        public static string Page(string fileName)
        {
            string path = Path.Combine(appPath, "Templates", fileName);

            if (!File.Exists(path))
                return "<p>Datei nicht gefunden: <i>" + path + "</i><p>";

            return System.IO.File.ReadAllText(path);
        }

        /// <summary>
        /// POST-Inhalte lesen
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Key-Value-Pair</returns>
        public static Dictionary<string, string> Payload(IHttpContext context)
        {
            System.IO.Stream body = context.Request.InputStream;
            System.IO.StreamReader reader = new System.IO.StreamReader(body);

            string[] pairs = reader.ReadToEnd().Split('&');

            Dictionary<string, string> payload = new Dictionary<string, string>();

            foreach (var pair in pairs)
            {
                string[] item = pair.Split('=');

                if (item.Length > 1)
                    payload.Add(item[0], WebUtility.UrlDecode(item[1]));
            }

            return payload;
        }

        public static string[] GetArrayPayload(IHttpContext context)
        {
            System.IO.Stream body = context.Request.InputStream;
            System.IO.StreamReader reader = new System.IO.StreamReader(body);

            string[] pairs = reader.ReadToEnd().Replace("\"", "").TrimStart('[').TrimEnd(']').Split(',');

            return pairs;
        }

    }
}
