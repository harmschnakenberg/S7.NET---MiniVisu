using Grapevine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace S7.NET.web
{
    partial class Html
    {
        internal static readonly string appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        private static readonly Dictionary<int, FileInfo> HtmlPagePathDict = new Dictionary<int, FileInfo>();

        /// <summary>
        /// GUID - User-Id
        /// </summary>
        internal static Dictionary<string, User> LogedInHash = new Dictionary<string, User>();


        public static Dictionary<string, string> ReadCookies(IHttpContext context)
        {
            Dictionary<string, string> cookies = new Dictionary<string, string>();

            foreach (Cookie cookie in context.Request.Cookies)
            {
                cookies.Add(cookie.Name, cookie.Value);
            }

            return cookies;
        }

        /// <summary>
        /// Dateien der Form '000-BeschreibenderName.html'
        /// Die führende Zahl muss dreistellig sein.
        /// </summary>
        /// <param name="folder">Ordnername</param>
        /// <param name="fileExtention">Dateiendung</param>
        private static void GetHtmlPagePathDict(string folder, string fileExtention = "*.html")
        {
            DirectoryInfo d = new DirectoryInfo(Path.Combine(appPath, folder));

            foreach (var file in d.GetFiles(fileExtention))
            {
                string[] n = file.Name.Split('-');

                if (n.Length > 1 && n[0].Length == 3 && int.TryParse(n[0], out int id))
                    HtmlPagePathDict.Add(id, file);
            }
        }


        /// <summary>
        /// Lade eine Datei anhand des Ablageorts und ersetze ggf. Zeichenketten
        /// </summary>
        /// <param name="folder"></param>
        /// <param name="fileName"></param>
        /// <param name="insert"></param>
        /// <returns></returns>
        public static string Page(string folder, string fileName, Dictionary<string, string> insert = null)
        {
            string path = Path.Combine(appPath, folder, fileName);

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


        /// <summary>
        /// Lade eine Seite anhand ihres Index
        /// </summary>
        /// <param name="folderName">Ordner, in dem die HTML-Datei zu finden ist</param>
        /// <param name="id">Führende Zahl im Dateinamen der HTML-Datei</param>
        /// <returns></returns>
        public static string Page(string folderName, int id)
        {
            if (HtmlPagePathDict.Count == 0)
                GetHtmlPagePathDict(folderName);

            if (!HtmlPagePathDict.ContainsKey(id)) //Wenn es die Datei nicht gibt, zurück ins Haupt-Menü
                id = 0;

            if (HtmlPagePathDict.ContainsKey(id))
            {
                string path = HtmlPagePathDict[id].FullName;

                if (File.Exists(path))
                    return System.IO.File.ReadAllText(path);
                else
                    return "<p>Dateipfad nicht gefunden: <i>" + path + "</i><p>";
            }

            return $"<p>Datei mit ID {id} ist unbekannt.<p>";
        }


        /// <summary>
        /// POST-Inhalte lesen
        /// </summary>
        /// <param name="context"></param>
        /// <returns>Key-Value-Pair</returns>
        public static Dictionary<string, string> PayloadForm(IHttpContext context)
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

        public static string[] PayloadArray(IHttpContext context)
        {
            System.IO.Stream body = context.Request.InputStream;
            System.IO.StreamReader reader = new System.IO.StreamReader(body);

            string[] pairs = reader.ReadToEnd().Replace("\"", "").TrimStart('[').TrimEnd(']').Split(',');

            return pairs;
        }

        public static Dictionary<string, string> PayloadJson(IHttpContext context)
        {
            System.IO.Stream body = context.Request.InputStream;
            System.IO.StreamReader reader = new System.IO.StreamReader(body);
            Dictionary<string, string> objs = new Dictionary<string, string>();

            string[] pairs = reader.ReadToEnd().Replace("\"", "").TrimStart('{').TrimEnd('}').Split(',');

            foreach (var pair in pairs)
            {
                string[] items = pair.Split(':');
                if (items.Length > 1)
                    objs.Add(items[0], items[1]);
            }

            return objs;
        }


        public static string GetHtmlAlarmList(List<AlarmTag> alarmTags)
        {
            StringBuilder html = new StringBuilder("<ul class='w3-ul'>");

            foreach (AlarmTag tag in alarmTags)
                html.AppendLine($"<li class='SW Prio{tag.Prio}' id='{tag.Name}'>{tag.Comment}<span class='w3-right'>{tag.Prio}</span></li>");

            html.AppendLine("</ul>");

            return html.ToString();
        }


        public static string Alert(int prio, string header, string content)
        {
            StringBuilder sb = new StringBuilder("<div class='w3-panel w3-border ");

            switch (prio)
            {
                case 1:
                    sb.Append("w3-pale-red'>");
                    break;
                case 2:
                    sb.Append("w3-pale-yellow'>");
                    break;
                case 3:
                    sb.Append("w3-pale-green'>");
                    break;
                default:
                    sb.Append("w3-pale-blue'>");
                    break;
            }

            sb.Append($"<h3>{header}</h3>");
            sb.Append($"<p>{content}</p></div>");

            return sb.ToString();
        }

        public static string SubmitButton(string label, string cssClass, string url = "")
        {
            StringBuilder sb = new StringBuilder("<input type='submit' ");
            sb.Append($"class='w3-button w3-quarter w3-section w3-ripple w3-padding w3-margin {cssClass}'");
            sb.Append($"value='{label}'");

            if (url.Length > 1)
                sb.Append($"formaction='{url}'");

            sb.Append(">");

            return sb.ToString();
        }
  
        /// <summary>
        /// Liest den angemeldeten Benutzer aus. Wenn kein passender Eintrag gefunden wird Benutzer 'None', AccessLevel 0
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal static User GetUserFromCookie(IHttpContext context)
        {
            User user = new User
            {
                Name = "None",
                AccessLevel = 0
            };

            if (ReadCookies(context).TryGetValue("WebVisuId", out string guid))
                _ = Server.LogedInHash.TryGetValue(guid, out user);
     
            return user;
        }
    }

}
