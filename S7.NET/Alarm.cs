using System;
using System.Collections.Generic;

namespace S7.NET
{
    class Alarm
    {
        /// <summary>
        /// Liste aller Alarm-Variablen
        /// </summary>
        public static List<AlarmTag> AlarmTags = new List<AlarmTag>();

        /// <summary>
        /// Lädt die zu überwachenden Alarm-Datenpunkte aus einer CSV-Datei
        /// </summary>
        /// <param name="csvFilePath">CSV Format: ItemName;AlarmComment;AlarmPrio </param>
        public static void LoadAlarmDb(string csvFilePath)
        {
            if (!System.IO.File.Exists(csvFilePath))
            {
                Console.WriteLine($"Die Datei <{csvFilePath}> konnte nicht gefunden werden.");
                return;
            }

            string[] lines = System.IO.File.ReadAllLines(csvFilePath);

            foreach (string line in lines)
            {
                string[] cols = line.Split(';');

                //mind. 3 Spalten, zweite Spalte nicht leer, dritte Splate ist Zahl
                if (cols.Length > 2 && cols[1].Trim().Length > 0 && int.TryParse(cols[2].Trim(), out int prio))
                    AlarmTags.Add(new AlarmTag() { Name = cols[0].Trim(), Comment = cols[1].Trim(), Prio = prio });
                else
                    Console.WriteLine("Alarm-Variable nicht lesbar aus: " + line);

            }
        }
    }

    public class AlarmTag
    {
        #region Basic Properties
        public string Name { get; set; }
        public bool Value { get; set; }
        #endregion

        public string Comment { get; set; }
        public int Prio { get; set; }
    }
}
