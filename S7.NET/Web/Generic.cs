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

        private static readonly Dictionary<int, FileInfo> HtmlPagePaths = new Dictionary<int, FileInfo>();

        /// <summary>
        /// Dateien der Form '000-BeschreibenderName.html'
        /// Die führende Zahl muss dreistellig sein.
        /// </summary>
        /// <param name="folder">Ordnername</param>
        /// <param name="fileExtention">Dateiendung</param>
        private static void GetHtmlPagePaths(string folder, string fileExtention = "*.html")
        {
            DirectoryInfo d = new DirectoryInfo(Path.Combine(appPath, folder));

            foreach (var file in d.GetFiles(fileExtention))
            {
                string[] n = file.Name.Split('-');

                if (n.Length > 1 && n[0].Length == 3 && int.TryParse(n[0], out int id))                
                    HtmlPagePaths.Add(id, file);
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
            if (HtmlPagePaths.Count == 0)
                GetHtmlPagePaths(folderName);

            if (!HtmlPagePaths.ContainsKey(id)) //Wenn es die Datei nicht gibt, zurück ins Haupt-Menü
                id = 0;

            if (HtmlPagePaths.ContainsKey(id))
            {
                string path = HtmlPagePaths[id].FullName;

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
