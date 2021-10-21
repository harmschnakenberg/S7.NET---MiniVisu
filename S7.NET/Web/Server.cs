using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grapevine;

namespace S7.NET
{
    class Server
    {
        private static IRestServer restServer;

        public static void Start()
        {
            if (restServer != null && restServer.IsListening) return;

            try
            {
                restServer = RestServerBuilder.From<Startup>().Build();

                restServer.AfterStarting += (s) =>
                {
                    Process.Start("explorer", s.Prefixes.First().Replace("+", System.Net.Dns.GetHostName()));
                    Console.WriteLine("Web-Server gestartet.");
                };

                restServer.AfterStopping += (s) =>
                {
                    Console.WriteLine("Web-Server beendet.");
                };

                restServer.Start();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void Stop()
        {
            if (restServer != null)
                restServer.Stop();
        }

    }
}
