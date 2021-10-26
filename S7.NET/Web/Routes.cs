using Grapevine;
using S7.Net.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        [RestRoute("Get", "/status/{pageTitle}")] //
        public static async Task GetStatusBar(IHttpContext context)
        {
            string title = System.Net.WebUtility.UrlDecode(context.Request.PathParameters["pageTitle"]); //URL Sonderzeichen behandeln
            Dictionary<string, string> pairs = new Dictionary<string, string>() {
                { "#TITEL#", title }
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

        [RestRoute] //Home-Seite
        public static async Task Home(IHttpContext context)
        {
            string html = Html.Page("html", 0);
            await context.Response.SendResponseAsync(html).ConfigureAwait(false);
        }

    }

}
