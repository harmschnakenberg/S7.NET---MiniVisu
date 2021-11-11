using Grapevine;
using S7.Net.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DateTime = System.DateTime;

namespace S7.NET.web
{
    [RestResource]
    class Routes    
    {
        [RestRoute("Post", "/api")] //Lese TagValues aus SPS
        public static async Task TestRec(IHttpContext context)
        {
            try
            {
                string[] payload = Html.PayloadArray(context);
                List<DataItem> dataItems = Program.ItemNames2DataItems(payload.ToList());                
                string output = await Program.PlcReadOnceAsync(Program.Plc1, dataItems);
                
                context.Response.ContentType = "application/json; charset=utf-8";
                context.Response.AddHeader("x-content-type-options", "nosniff");
                context.Response.AddHeader("cache-control", "no-cache");
                context.Response.ContentEncoding = System.Text.Encoding.UTF8;

                await context.Response.SendResponseAsync(output).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                //await context.Response.SendResponseAsync(ex.Message + Environment.NewLine + ex.StackTrace).ConfigureAwait(false);
                await context.Response.SendResponseAsync(string.Empty).ConfigureAwait(false);

                throw ex;
            }
        }

        [RestRoute("Post", "/api/write")] //Schreibe TagValue in SPS
        public static async Task WriteTagValue(IHttpContext context)
        {
           Dictionary<string, string> payload = Html.PayloadJson(context);
            if (payload.TryGetValue("Name", out string tagName) &&
                payload.TryGetValue("Value", out string val))
                    Program.PlcWriteOnceAsync(Program.Plc1, tagName, val);

            context.Response.ContentType = "application/json";
            context.Response.AddHeader("x-content-type-options", "nosniff");
            context.Response.AddHeader("cache-control", "no-cache");
            context.Response.ContentEncoding = System.Text.Encoding.UTF8;
            context.Response.StatusCode = 200;

            await context.Response.SendResponseAsync(string.Empty).ConfigureAwait(false);
        }

        [RestRoute("Get", "/script/read.js")] //Lädt Javascript zum nachladen aktueller TagValues
        public static async Task ScriptReadTags(IHttpContext context)
        {
            context.Response.AddHeader("Cache-Control", "no-cache"); // "max-age=31536000, immutable"
            await context.Response.SendResponseAsync(Html.Page("js", "ReadTags.js")).ConfigureAwait(false);
        }

        [RestRoute("Get", "/statusbar/{pageTitle}")] //
        public static async Task GetStatusBar(IHttpContext context)
        {
            string title = System.Net.WebUtility.UrlDecode(context.Request.PathParameters["pageTitle"]); //URL Sonderzeichen behandeln
            User user = null;
            if (Html.ReadCookies(context).TryGetValue("WebVisuId", out string guid))
                Server.LogedInHash.TryGetValue(guid, out user);
            
            Dictionary<string, string> pairs = new Dictionary<string, string>() {
                { "#TITEL#", title },
                { "#LOGEDIN#", user is null ? string.Empty : "w3-green" },
                { "#USERNAME#", $"Angemeldet: {user?.Name??  "-niemand-"} [{user?.AccessLevel?? 0}]" }
            };

            string html = Html.Page("html", "RandOben.html", pairs);

            context.Response.AddHeader("Cache-Control", "max-age=31536000, immutable"); 
            await context.Response.SendResponseAsync(html).ConfigureAwait(false);
        }

        [RestRoute("Get", "/{pageId:num}")] //Ruft eine html-Seite anhand ihres Index auf
        public static async Task GetPage(IHttpContext context)
        {
            _ = int.TryParse(context.Request.PathParameters["pageId"], out int pageId);
            
            string html = Html.Page("html", pageId);

            context.Response.AddHeader("Cache-Control", "no-cache"); // "max-age=31536000, immutable"
            await context.Response.SendResponseAsync(html).ConfigureAwait(false);
        }

        [RestRoute("Get", "/alarm")] //Ruft eine dynamsch erstellte Alarm-Liste auf
        public static async Task GetAlertPage(IHttpContext context)
        {
            string htmlAlarmList = Html.GetHtmlAlarmList(Alarm.AlarmTags);
            Dictionary<string, string> pairs = new Dictionary<string, string>() { { "#ALARMLIST#", htmlAlarmList } };
            string html = Html.Page("html", "Alarm.html", pairs);

            await context.Response.SendResponseAsync(html).ConfigureAwait(false);
        }
       
        #region Benutzerverwaltung
        [RestRoute("Get", "/benutzer")] //Ruft die Benutzerverwaltung auf
        public static async Task GetAccountsPage(IHttpContext context)
        {
            User user = Html.GetUserFromCookie(context);
            string htmlUserTable = Html.GetHtmlUserTable(user.AccessLevel);

            string htmlButtons = string.Empty; // Html.SubmitButton("Speichern", "w3-blue");
            if (user.AccessLevel >= Server.AccessLevelSystem)
                htmlButtons += Html.SubmitButton("Neu", "w3-green", "/benutzer/neu")
                    + Html.SubmitButton("Löschen", "w3-red", "benutzer/loeschen");

            Dictionary<string, string> pairs = new Dictionary<string, string>() { 
                { "#USERSTABLE#", htmlUserTable },
                { "#BUTTON#",   htmlButtons}
            };
            string html = Html.Page("html", "Benutzer.html", pairs);

            await context.Response.SendResponseAsync(html).ConfigureAwait(false);
        }

        [RestRoute("Post", "/benutzer/neu")]
        public static async Task CreateNewAccount(IHttpContext context)
        {
            User user = Html.GetUserFromCookie(context);

            Dictionary<string, string> payload = Html.PayloadForm(context);
            string name = payload["accountName"];
            string accessLevelStr = payload["accountAccessLevel"];
            string password1 = payload["accountPassword1"];
            string password2 = payload["accountPassword2"];
                        
            string alert;
            #region Berechtigung prüfen
            if (user.AccessLevel < Server.AccessLevelSystem || !int.TryParse(accessLevelStr, out int accessLevel) || user.AccessLevel < accessLevel)
                alert = Html.Alert(1, "Keine Berechtigung", "Sie haben keine Berechtigung für diese Aktion.");
            else if (password1.Length < 1 || password1 != password2)
                alert = Html.Alert(2, "Fehlerhafte Eingabe", "Bitte Passworteingabe prüfen.");
            //else if (!Html.IsUserNameUnique(name))
            //    alert = Html.Alert(1, "Benutzer bereits vorhanden", "Diesen Benutzer gibt es schon.");
            else if (!Html.CreateUser(name, accessLevel, password1))
                alert = Html.Alert(1, "Benutzer anlegen fehlgeschlagen", "Es ist ein Fehler beim Speichern des Benutzers aufgetreten.");
            else
                alert = Html.Alert(3, "Benutzer angelegt", $"Der Benutezr '{name}' wurde erfolgreich angelegt.");

            string htmlButtons = string.Empty; // Html.SubmitButton("Speichern", "w3-blue", "/benutzer/aendern");
            if (user.AccessLevel >= Server.AccessLevelSystem)
                htmlButtons += Html.SubmitButton("Neu", "w3-green", "/benutzer/neu")
                    + Html.SubmitButton("Löschen", "w3-red", "benutzer/loeschen");
            #endregion

            string htmlUserTable = Html.GetHtmlUserTable(user.AccessLevel);

            Dictionary<string, string> pairs = new Dictionary<string, string>() {
                { "#USERSTABLE#", alert + htmlUserTable },
                { "#BUTTON#",   htmlButtons}
            };

            string html = Html.Page("html", "Benutzer.html", pairs);

            await context.Response.SendResponseAsync(html).ConfigureAwait(false);
        }

        [RestRoute("Post", "/benutzer/loeschen")]
        public static async Task UpdateAccount(IHttpContext context)
        {
            User user = Html.GetUserFromCookie(context);

            Dictionary<string, string> payload = Html.PayloadForm(context);
            string name = payload["accountName"];
           
            string alert;
            #region Berechtigung prüfen
            if (user.AccessLevel < Server.AccessLevelSystem)
                alert = Html.Alert(1, "Keine Berechtigung", "Sie haben keine Berechtigung für diese Aktion.");
            else if (!Html.DeleteUser(name))
                alert = Html.Alert(1, "Fehler beim Löschen des Benutzers", $"Der Benutezr '{name}' konnte nicht gelöscht werden.");
            else
                alert = Html.Alert(3, "Fehler beim Löschen des Benutzers", $"Der Benutezr '{name}' konnte nicht gelöscht werden.");

            string htmlButtons = string.Empty; // Html.SubmitButton("Speichern", "w3-blue");
            if (user.AccessLevel >= Server.AccessLevelSystem)
                htmlButtons += Html.SubmitButton("Neu", "w3-green", "/benutzer/neu")
                    + Html.SubmitButton("Löschen", "w3-red", "benutzer/loeschen");
            #endregion

            string htmlUserTable = Html.GetHtmlUserTable(user.AccessLevel);


            Dictionary<string, string> pairs = new Dictionary<string, string>() {
                { "#USERSTABLE#", alert + htmlUserTable },
                 { "#BUTTON#", htmlButtons }
            };

            string html = Html.Page("html", "Benutzer.html", pairs);

            await context.Response.SendResponseAsync(html).ConfigureAwait(false);
        }

        #endregion

        [RestRoute("Post", "/login")]
        public static async Task Login(IHttpContext context)
        {
            Dictionary<string, string> payload = Html.PayloadForm(context);
            string name = payload["username"];
            string password = payload["psw"];
            string currentPageUrl = payload["currentPage"];
            string guid = Html.CheckCredentials(name, password);
           
            System.Net.Cookie cookie = new System.Net.Cookie("WebVisuId", guid, "/");
            context.Response.Cookies.Add(cookie);

            context.Response.Redirect(currentPageUrl);
            await context.Response.SendResponseAsync().ConfigureAwait(false);
        }

        [RestRoute] //Home-Seite
        public static async Task Home(IHttpContext context)
        {
            string html = Html.Page("html", 0);
            await context.Response.SendResponseAsync(html).ConfigureAwait(false);
        }

    }

}
