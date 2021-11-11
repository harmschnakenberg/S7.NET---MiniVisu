using Newtonsoft.Json;
using S7.Net;
using S7.Net.Types;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace S7.NET
{
    partial class Program
    {
        public static CancellationTokenSource CancelSource = new CancellationTokenSource();

        static void Main()
        {
            Plc1.OpenAsync();
            Server.Start();
            //G:\VisualStudio\Projekte\S7.NET\S7.NET\S7.NET\csv\DB99.csv
            Alarm.LoadAlarmDb(System.IO.Path.Combine(S7.NET.web.Html.appPath,"csv", "DB99.csv"));
            S7.NET.web.Html.CreateUser("Harm", 9000, "7307");


            #region Testfeld 'Dauerhaft lesen'
            //#region Dummy Datenerzeugung
            //List<string> writeTags = new List<string>
            //{
            //     "DB10.DBW10", 
            //     "DB10.DBW12", 
            //     "DB10.DBW14", 
            //};

            //string json = JsonConvert.SerializeObject(writeTags);

            //Console.WriteLine("Anfrage-String:");
            //Console.WriteLine(json);

            //List<string> readTags = JsonConvert.DeserializeObject<List<string>>(json);
            //List<DataItem> dataItems = ItemNames2DataItems(readTags);
            //#endregion

            //try
            //{
            //    PlcRead(Plc1, dataItems, CancelSource.Token, 0); //Dauerlesen

            //    TimeoutRead(60000);

            //}
            //catch (PlcException plc_ex)
            //{
            //    Console.WriteLine($"Fehler {plc_ex.ErrorCode}: {plc_ex.Message}");
            //}

            Console.WriteLine("Programmende.");
            Console.ReadKey();
            CancelSource.Cancel(); //Dauerhafte Abfrage beenden
            #endregion
        }


        //private static void TimeoutRead(int milSec)
        //{
        //    System.Timers.Timer timer = new System.Timers.Timer
        //    {
        //        Interval = milSec,
        //        AutoReset = false
        //    };
        //    timer.Elapsed += Timer_Elapsed;
        //    timer.Start();
        //}

        //private static void Timer_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    CancelSource.Cancel(); //Dauerhafte Abfrage beend
        //}
    }


}