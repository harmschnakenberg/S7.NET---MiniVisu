using Newtonsoft.Json;
using S7.Net;
using S7.Net.Types;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


namespace S7.NET
{
    partial class Program
    {
        public static Plc Plc1 = new Plc(CpuType.S71500, "192.168.160.56", 0, 1);

        public static void PlcRead(Plc plc, List<DataItem> tags, CancellationToken cancelToken, int readIntervall = 0)
        {
            new Thread(async () =>
            {
                plc.Open();

                do
                {
                    if (cancelToken.IsCancellationRequested) //für dauerhafte Abfragen
                        plc.Close();

                    if (!plc.IsConnected) return;

                    int index = 0;
                    int end = tags.Count;
                    while (index < end) //Es können max. 20 Tags in einer Abfrage sein
                    {
                        #region max. 20 Tags in einer Abfrage
                        int count = Math.Min(end - index, 20);
                        List<DataItem> range = tags.GetRange(index, count);
                        index += count;
                        #endregion

                        _ = await plc.ReadMultipleVarsAsync(range, cancelToken);

                       Console.WriteLine(Tags2Json(range)); // TODO: Das muss noch an REST-API geleitet werden.
                    }

                    System.Threading.Thread.Sleep(readIntervall); //für dauerhafte Abfragen

                } while (readIntervall > 0);

            }).Start();

            plc.Close();
        }


        public static async Task<string> PlcReadOnceAsync(Plc plc, List<DataItem> tags)
        {           
            try
            {
                
                if (!plc.IsConnected)
                {
                    plc.Close();
                    plc.Open();
                }

                if (!plc.IsConnected) return string.Empty;

                List<DataItem> outTags = new List<DataItem>();

                int index = 0;
                int end = tags.Count;
                while (index < end) //Es können im S7-Protokoll max. 20 Tags in einer Abfrage sein
                {
                    #region max. 20 Tags in einer Abfrage
                    int count = Math.Min(end - index, 19);
                    List<DataItem> range = tags.GetRange(index, count);
                    index += count;
                    #endregion

                    _ = await plc.ReadMultipleVarsAsync(range);

                    outTags.AddRange(range);
                }

                // plc.Close(); //böse! Bricht nach ca. 1 min mit NullReferenceException ab.

                return Tags2Json(outTags);
            }
            catch (Exception ex)
            {
                plc.Close();
                throw new Exception("PlcReadOnceAsync() " + ex.Message);
            }
        }

        public static List<DataItem> ItemNames2DataItems(List<string> itemNames)
        {
            List<DataItem> dataItems = new List<DataItem>();

            foreach (var itemName in itemNames)
                if (itemName.Length > 0)
                    dataItems.Add(DataItem.FromAddress(itemName));

            return dataItems;
        }

        public static string Tags2Json(List<DataItem> dataItems)
        {
            List<Tag> readTags = new List<Tag>();

            foreach (var item in dataItems)
            {
                string itemName;
                string offset = string.Empty;

                switch (item.VarType)
                {
                    case VarType.Bit:
                        offset = $"DBX{item.StartByteAdr}.{item.BitAdr}";
                        break;
                    case VarType.Byte:
                        offset = $"DBB{item.StartByteAdr}";
                        break;
                    case VarType.Word:
                    case VarType.Int:
                        offset = $"DBW{item.StartByteAdr}";
                        break;
                    case VarType.DWord:
                    case VarType.DInt:
                        break;
                    case VarType.Real:
                        offset = $"DBD{item.StartByteAdr}";
                        break;
                        //case VarType.LReal:
                        //    break;
                        //case VarType.String:
                        //    break;
                        //case VarType.S7String:
                        //    break;
                        //case VarType.S7WString:
                        //    break;
                        //case VarType.Timer:
                        //    break;
                        //case VarType.Counter:
                        //    break;
                        //case VarType.DateTime:
                        //    break;
                        //case VarType.DateTimeLong:
                        //    break;
                        //default:
                        //    break;
                }

                switch (item.DataType)
                {
                    case DataType.DataBlock:
                        itemName = $"DB{item.DB}.{offset}";
                        break;
                    case DataType.Input:
                        itemName = "Eingang";
                        break;
                    case DataType.Output:
                        itemName = "Ausgang";
                        break;
                    case DataType.Memory:
                        itemName = "Merker";
                        break;
                    case DataType.Timer:
                        itemName = "Timer";
                        break;
                    case DataType.Counter:
                        itemName = "Counter";
                        break;
                    default:
                        itemName = "Unbekannt";
                        break;
                }

                if (itemName.Length > 0) // && item.Value.ToString().Length > 0)
                    readTags.Add(new Tag() { Name = itemName, Value = item.Value } );
            }

            string json = JsonConvert.SerializeObject(readTags);

            return json;
        }


        public static void PlcWriteOnceAsync(Plc plc, string tagName, string tagValue)
        {
            tagValue = tagValue.Trim();

            Console.WriteLine($"Scheibe {tagName} = {tagValue}");
            try
            {
                if (!plc.IsConnected)
                {
                    plc.Close();
                    plc.Open();
                }

                if (!plc.IsConnected) return;

                string[] tag = tagName.Split('.');

                if (tag.Length == 1)
                    return;
                else if (tag.Length == 2)
                {
                    if (tag[1].StartsWith("DBD") && double.TryParse(tagValue, out double valReal)) //REAL
                        plc.Write(tagName, valReal);
                    else if (tag[1].StartsWith("DBW") && short.TryParse(tagValue, out short valShort)) //INT; WORD = ushort
                        plc.Write(tagName, valShort);
                    else if (tag[1].StartsWith("DINT") && int.TryParse(tagValue, out int valInt)) //DINT
                        plc.Write(tagName, valInt);
                }
                else if (tag.Length > 2)
                {
                    if (tagValue == "true")
                        plc.Write(tagName, true);
                    else if (tagValue == "false")
                        plc.Write(tagName, false);
                }
   

            }
            catch (PlcException plc_ex)
            {
                Console.WriteLine(plc_ex.Message);
                plc.Close();
            }
            catch (Exception ex)
            {
                plc.Close();
                Console.WriteLine(ex.Message);
                throw new Exception("PlcWriteOnceAsync() " + ex.Message);
            }
        }

    }
}

public class Tag
{
    #region Basic Properties
    public string Name { get; set; }
    public object Value { get; set; }
    #endregion
}