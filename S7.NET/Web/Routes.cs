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
                string[] payload = Html.GetArrayPayload(context);
                List<DataItem> dataItems = Program.ItemNames2DataItems(payload.ToList());                
                string output = await Program.PlcReadOnceAsync(Program.Plc1, dataItems);
                
                context.Response.ContentType = "application/json";
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

        [RestRoute("Get", "/script/read.js")] //Lädt Javascript zum nachladen aktueller TagValues
        public static async Task ScriptReadTags(IHttpContext context)
        {
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

            await context.Response.SendResponseAsync(html).ConfigureAwait(false);
        }

        [RestRoute("Get", "/{pageId:num}")] //Ruft eine html-Seite anhand ihres Index auf
        public static async Task GetPage(IHttpContext context)
        {
            _ = int.TryParse(context.Request.PathParameters["pageId"], out int pageId);
            
            string html = Html.Page("html", pageId);

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

        [RestRoute("Get", "/benutzer")] //Ruft die Benutzerverwaltung auf
        public static async Task GetAccountsPage(IHttpContext context)
        {
            int userAccessLevel = 0;
            if (Html.ReadCookies(context).TryGetValue("WebVisuId", out string guid) &&
                Server.LogedInHash.TryGetValue(guid, out User user))
                    userAccessLevel = user.AccessLevel;

            string htmlUserTable = Html.GetHtmlUserTable(userAccessLevel);

            string htmlButtons = Html.SubmitButton("Speichern", "w3-blue");
            if (userAccessLevel >= Server.AccessLevelSystem)
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
            int userAccessLevel = 0;
            if (Html.ReadCookies(context).TryGetValue("WebVisuId", out string guid) &&
                Server.LogedInHash.TryGetValue(guid, out User user))
                userAccessLevel = user.AccessLevel;

            Dictionary<string, string> payload = Html.Payload(context);
            string name = payload["accountName"];
            string accessLevelStr = payload["accountAccessLevel"];
            string password1 = payload["accountPassword1"];
            string password2 = payload["accountPassword2"];
                        
            string alert;

            if (userAccessLevel < 1 || !int.TryParse(accessLevelStr, out int accessLevel) || userAccessLevel < accessLevel)
                alert = Html.Alert(1, "Keine Berechtigung", "Sie haben keine Berechtigung für diese Aktion.");
            else if (password1.Length < 1 || password1 != password2)
                alert = Html.Alert(2, "Fehlerhafte Eingabe", "Bitte Passworteingabe prüfen.");
            else if (!Html.IsUserNameUnique(name))
                alert = Html.Alert(1, "Benutzer bereits vorhanden", "Diesen Benutzer gibt es schon.");
            else if (!Html.RegisterUser(name, accessLevel, password1))
                alert = Html.Alert(1, "Benutzer anlegen fehlgeschlagen", "Es ist ein Fehler beim Speichern des Benutzers aufgetreten.");
            else
                alert = Html.Alert(3, "Benutzer angelegt", $"Der Benutezr '{name}' wurde erfolgreich angelegt.");

            string htmlUserTable = Html.GetHtmlUserTable(userAccessLevel);

            string htmlButtons = Html.SubmitButton("Speichern", "w3-blue");
            if (userAccessLevel >= Server.AccessLevelSystem)
                htmlButtons += Html.SubmitButton("Neu", "w3-green", "/benutzer/neu")
                    + Html.SubmitButton("Löschen", "w3-red", "benutzer/loeschen");

            Dictionary<string, string> pairs = new Dictionary<string, string>() {
                { "#USERSTABLE#", alert + htmlUserTable },
                { "#BUTTON#",   htmlButtons}
            };

            string html = Html.Page("html", "Benutzer.html", pairs);

            await context.Response.SendResponseAsync(html).ConfigureAwait(false);
        }


        [RestRoute("Post", "/login")]
        public static async Task Login(IHttpContext context)
        {
            Dictionary<string, string> payload = Html.Payload(context);
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
