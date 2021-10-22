using Grapevine;
using Newtonsoft.Json;
using S7.Net.Types;
using System;
using System.Collections.Generic;
using System.IO;
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
            try
            {
                string[] payload = Html.GetArrayPayload(context);
                List<DataItem> dataItems = Program.ItemNames2DataItems(payload.ToList());

                string output = await Program.PlcReadOnceAsync(Program.Plc1, dataItems);
                await context.Response.SendResponseAsync(output).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw ex;
                //await context.Response.SendResponseAsync(string.Empty).ConfigureAwait(false);
            }
        }

        [RestRoute("Get", "/script/read.js")]
        public static async Task ScriptReadTags(IHttpContext context)
        {   
            await context.Response.SendResponseAsync(Html.Page("ReadTags.js")).ConfigureAwait(false);
        }


        [RestRoute]
        public static async Task Home(IHttpContext context)
        {
            string html = Html.Page("index.html", null);
            await context.Response.SendResponseAsync(html).ConfigureAwait(false);
        }

    }

}
