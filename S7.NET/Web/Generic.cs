using Grapevine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace S7.NET.web
{
    class Html
    {
        internal static readonly string appPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        private static readonly Dictionary<int, FileInfo> HtmlPagePathDict = new Dictionary<int, FileInfo>();

        /// <summary>
        /// GUID - User-Id
        /// </summary>
        internal static Dictionary<string, Person> LogedInHash = new Dictionary<string, Person>();


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


        public static string GetHtmlAlarmList(List<AlarmTag> alarmTags)
        {
            StringBuilder html = new StringBuilder("<ul class='w3-ul'>");

            foreach (AlarmTag tag in alarmTags)
                html.AppendLine($"<li class='SW Prio{tag.Prio}' id='{tag.Name}'>{tag.Comment}<span class='w3-right'>{tag.Prio}</span></li>");

            html.AppendLine("</ul>");

            return html.ToString();
        }

        internal static string CheckCredentials(string name, string password)
        {
            try
            {
                #region Benutzer-Passwort-Kombination finden
                string guid = string.Empty;
                string encryped_pw = Encrypt(password);
                int accessLevel = 0;

                string path = Path.Combine(appPath, "csv", "Credentials.csv");

                if (File.Exists(path))
                {
                    string[] lines = System.IO.File.ReadAllLines(path);

                    foreach (string line in lines)
                    {
                        if (line.StartsWith(name))
                        {
                            string[] items = line.Split(';');

                            if (items.Length > 2 && items[2].Trim() == encryped_pw)
                            {
                                guid = Guid.NewGuid().ToString("N");
                                int.TryParse(items[1], out accessLevel);
                                break;
                            }
                        }
                    }
                }
                #endregion

                #region Liste eingeloggter Benutzer aktualisieren
                if (guid.Length > 0)
                {
                    while (Server.LogedInHash.Count > 10) //Max. 10 Benutzer gleichzetig eingelogged
                    {
                        Server.LogedInHash.Remove(Server.LogedInHash.Keys.GetEnumerator().Current);
                    }

                    User user = new User
                    {
                        Name = name,
                        AccessLevel = accessLevel
                    };

                    Server.LogedInHash.Add(guid, user);

                    return guid;
                }
                #endregion
            }
            catch (Exception)
            {
                throw;
                // Was tun?
            }

            return string.Empty;
        }

        internal static bool RegisterUser(string name, int level, string password)
        {
            try
            {
                string encryped_pw = Encrypt(password);
                string path = Path.Combine(appPath, "csv", "Credentials.csv");
                if (!File.Exists(path)) return false;

                string[] lines = System.IO.File.ReadAllLines(path);

                foreach (string line in lines)
                {
                    if (line.StartsWith(name))
                        return false;
                }

                File.AppendAllText(path, $"{name};{level};{encryped_pw}" + Environment.NewLine);
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
                //return false;
            }
        }

        internal static bool IsUserNameUnique(string name)
        {
            string path = Path.Combine(appPath, "csv", "Credentials.csv");
            if (!File.Exists(path)) return false;

            string[] lines = System.IO.File.ReadAllLines(path);

            foreach (string line in lines)
            {
                if (line.StartsWith(name))
                    return false;
            }

            return true;
        }


        private static string Encrypt(string password)
        {
            if (password == null) return password;

            byte[] data = System.Text.Encoding.UTF8.GetBytes(password);
            data = new System.Security.Cryptography.SHA256Managed().ComputeHash(data);
            return System.Text.Encoding.UTF8.GetString(data);
        }

        internal static string GetHtmlUserTable(int userAccessLevel)
        {
            try
            {
                string path = Path.Combine(appPath, "csv", "Credentials.csv");
                if (!File.Exists(path)) return "Die Benutzerdatei ist nicht vorhanden.";

                string[] lines = System.IO.File.ReadAllLines(path);
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<table class='w3-table-all w3-light-grey'>");

                foreach (string line in lines)
                {
                    string[] items = line.Split(';');

                    if (int.TryParse(items[1], out int accessLevel) && accessLevel >= userAccessLevel) //nur Benutzer die max userAccessLevel haben
                    {
                        sb.Append($"<tr><td onclick='getAccount(this.parentNode)' class='w3-button material-icons' style='width:50px;'>edit</td>");
                        sb.Append($"<td>{items[0]}</td><td>{accessLevel}</td></tr>");
                    }
                }

                sb.AppendLine("</table>");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw ex;
                //return false;
            }
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
    }

    public class Person
    {
        public string Name { get; set; }

        public int Level { get; set; }
    }
}
