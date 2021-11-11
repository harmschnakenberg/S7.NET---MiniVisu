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

        /// <summary>
        /// GUID - User-Id
        /// Liste der eingeloggten Benutzer
        /// </summary>
        internal static Dictionary<string, User> LogedInHash = new Dictionary<string, User>();

        public static int AccessLevelSystem { get; set; } = 9000;

        public static void Start()
        {
            if (restServer != null && restServer.IsListening) return;

            try
            {
                restServer = RestServerBuilder.From<Startup>().Build();

                foreach (GlobalResponseHeaders item in restServer.GlobalResponseHeaders)
                {
                    if (item.Name == "Server") //Default Global Header für Server führt zu Warnung in Edge
                        item.Value = "kreutztraeger";
                        item.Suppress = true;
                }

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

    public class User
    {
        public string Name { get; set; }

        public int AccessLevel { get; set; }

        public string EncyptedPassword { get; set; }

        //Hier können später noch Eigenschaften ergänzt werden (Berechtigung)

    }
}
