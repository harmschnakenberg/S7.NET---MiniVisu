using Grapevine;
using Newtonsoft.Json;
using S7.Net.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace S7.NET.web
{
    [RestResource]
    class Routes    
    {     
        [RestRoute("Post", "/api")]
        public static async Task TestRec(IHttpContext context)
        {
            string[] payload = Html.GetArrayPayload(context);
            List<DataItem> dataItems = Program.ItemNames2DataItems(payload.ToList());
            string output = await Program.PlcReadOnceAsync(Program.Plc1, dataItems);
            await context.Response.SendResponseAsync(output).ConfigureAwait(false);
        }

        [RestRoute]
        public static async Task Home(IHttpContext context)
        {
            string html = Html.Page("index.html", null);
            await context.Response.SendResponseAsync(html).ConfigureAwait(false);
        }

    }

}
