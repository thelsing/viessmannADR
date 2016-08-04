using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace viessmannAdr
{
    class Program
    {
        static Dictionary<string, string> textValues = new Dictionary<string, string>();
        static HashSet<string> setOfAdrForFile = new HashSet<string>();
        static DataClasses1DataContext dc = new DataClasses1DataContext();

        static void Main(string[] args)
        {
            initTextValues();
            //fetchAdrFromStatistics();
            fetchAdrFromEventGroup(19130);
            generateVitoXML();
            Console.ReadKey();
        }

        private static void fetchAdrFromEventGroup(int id)
        {
            var query = from g in dc.ecnEventTypeGroup
                        where g.DataPointTypeId == 350
                        && g.ParentId == -1
                        && (g.ecnEventTypeGroup2.Any() || g.ecnEventTypeEventTypeGroupLink.Any())
                        select g;

            if (id != 0)
                query = query.Where(g => g.Id == id);

            foreach (var g in query)
            {
                printEventGroup(0, g, id == 0);

                if (id == 0)
                {
                    Console.WriteLine();
                    Console.ReadKey();
                }
            }
        }

        private static void initTextValues()
        {
            XmlSerializer trSerializer = new XmlSerializer(typeof(TextResources));

            TextResources tr = (TextResources)trSerializer.Deserialize(new XmlTextReader(@"C:\Program Files\Viessmann Vitosoft 300 SID1\ServiceTool\Web\XmlDocuments\Textresource_de_tk.xml"));
            foreach (var item in tr.Items)
                textValues[item.Label] = item.Value;
        }

        static string trans(string para)
        {
            var ret = para.Trim('@');
            ret = ret.Replace("viessmann.eventvaluetype.name.", "viessmann.eventvaluetype.");
            if (textValues.ContainsKey(ret))
                ret = textValues[ret];

            return ret.Trim();
        }

        static void printEventGroup(int indent, ecnEventTypeGroup g, bool print)
        {
            var name = trans(g.Name);
            bool showResult = true;

            g.ecnDisplayConditionGroup.Load();
            var hideGroup = EvalConditions(indent, g.ecnDisplayConditionGroup, false);
            if (hideGroup && showResult)
                return;

            if(print)
                Console.WriteLine(new string(' ', indent) + "G (" + g.Id +") " + name + (hideGroup? " (Hidden) ":" ") + g.OrderIndex);
            EvalConditions(indent + 8, g.ecnDisplayConditionGroup, !showResult);

            g.ecnEventTypeEventTypeGroupLink.Load();
            foreach (var evl in g.ecnEventTypeEventTypeGroupLink.OrderBy(o => o.EventTypeOrder))
            {
                var evName = evl.ecnEventType.Name.Trim('@');
                if (textValues.ContainsKey(evName))
                    evName = textValues[evName];

                evl.ecnEventType.ecnDisplayConditionGroup.Load();
                var hide = EvalConditions(indent + 16, evl.ecnEventType.ecnDisplayConditionGroup, false);
                if (!hide || !showResult)
                {
                    if(print)
                        Console.WriteLine(new string(' ', indent + 8) + "E " + evName + (hide ? " (Hidden)" : ""));
                    EvalConditions(indent + 16, evl.ecnEventType.ecnDisplayConditionGroup, !showResult);
                    setOfAdrForFile.Add(evl.ecnEventType.Address);
                }
            }

            g.ecnEventTypeGroup2.Load();
            foreach (var sg in g.ecnEventTypeGroup2.OrderBy(o => o.OrderIndex))
                printEventGroup(indent + 8, sg, print);
        }

        private static bool EvalConditions(int indent, EntitySet<ecnDisplayConditionGroup> g, bool debug)
        {
            bool ret = false;
            foreach(var c in g)
            {
                c.ecnDisplayCondition.Load();
                bool cond = false;
                foreach (var co in c.ecnDisplayCondition)
                {
                    cond = co.ecnEventGroupValueCache.Any() && co.EqualCondition;
                    if (!co.EqualCondition)
                        cond = !cond;

                    if (cond)
                        break;
                }

                ret = cond;
                if (!debug)
                {
                    if (cond)
                        break;
                    else
                        continue;
                }

                var name = trans(c.Name);
                Console.WriteLine(new string(' ', indent) + "C " + name + " (" + cond + ")");
                foreach (var co in c.ecnDisplayCondition)
                {
                    var value = co.ecnEventGroupValueCache.Any();

                    if (!co.EqualCondition)
                        value = !value;

                    Console.WriteLine(new string(' ', indent + 8) + trans(co.ecnEventType.Name) + " " +
                        (co.EqualCondition ? "==" : "!=") + " " + trans(co.ecnEventValueType.Name) + 
                        " (" + value + ")");
                }
            }
            return ret;
        }

        static void generateVitoXML()
        {
            
            Dictionary<string, EventTypesEventType> evTypes = new Dictionary<string, EventTypesEventType>();
            Dictionary<string, string> textValues = new Dictionary<string, string>();

            XmlSerializer evSerializer = new XmlSerializer(typeof(EventTypes));
            XmlSerializer trSerializer = new XmlSerializer(typeof(TextResources));

            var evDict = new Dictionary<string, string>();
            var dc = new DataClasses1DataContext();
            var query = from t in dc.ecnDataPointTypeEventTypeLink
                        //where t.DataPointTypeId == 350
                        select new { t.ecnEventType.Address, t.ecnEventType.Name };
            foreach (var item in query)
                evDict[item.Address] = trans(item.Name);

            var allEvTypes = new Dictionary<string, EventTypesEventType>();
            EventTypes types = (EventTypes)evSerializer.Deserialize(new XmlTextReader(@"C:\Program Files\Viessmann Vitosoft 300 SID1\ServiceTool\MobileClient\Config\ecnEventType.xml"));
            foreach (var item in types.Items)
                allEvTypes[item.ID] = item;
            types = (EventTypes)evSerializer.Deserialize(new XmlTextReader(@"C:\Program Files\Viessmann Vitosoft 300 SID1\ServiceTool\MobileClient\Config\sysDeviceIdent.xml"));
            foreach (var item in types.Items)
                allEvTypes[item.ID] = item;
            types = (EventTypes)evSerializer.Deserialize(new XmlTextReader(@"C:\Program Files\Viessmann Vitosoft 300 SID1\ServiceTool\MobileClient\Config\sysDeviceIdentExt.xml"));
            foreach (var item in types.Items)
                allEvTypes[item.ID] = item;

            List<EventTypesEventType> wantedEvTypes = new List<EventTypesEventType>();
            int count = 0;
            foreach (var a in setOfAdrForFile)
            {
                var t = allEvTypes[a];
                if (!evDict.ContainsKey(t.ID) && !(t.Name != null && t.Name.StartsWith("sys")) || string.IsNullOrEmpty(t.Address))
                    continue;

                if (!setOfAdrForFile.Contains(t.ID))
                    continue;

                //if (t.BitLength == "0")
                //    continue;

                wantedEvTypes.Add(t);
                if (evDict.ContainsKey(t.ID))
                    t.Name = evDict[t.ID];

                if (t.ID.Contains('~'))
                {
                    t.ID = t.ID.Remove(t.ID.IndexOf('~'));
                }

                if (t.Description != null)
                {
                    t.Description = trans(t.Description).Replace("##ecnnewline##", "\n").Replace("##ecntab##", "\t");
                }
                if (t.ValueList != null)
                {
                    var builder = new StringBuilder();
                    var values = t.ValueList.Split(';');
                    foreach (var value in values)
                    {
                        var pair = value.Split('=');
                        builder.Append(pair[0]);
                        builder.Append("=");
                        builder.Append(trans(pair[1]));
                        builder.Append(";");
                    }
                    t.ValueList = builder.ToString().Trim(';');
                }
                if (t.Unit != null)
                {
                    t.Unit = trans(t.Unit);
                }
                count++;
            }
            Console.WriteLine("found " + count + "items");
            EventTypes toWriteOut = new EventTypes();
            toWriteOut.Items = wantedEvTypes.OrderBy(t => t.Address).ToArray();

            TextWriter writer = new StreamWriter("eventTypes.xml");
            evSerializer.Serialize(writer, toWriteOut);
            writer.Close();

            XElement commands = new XElement("commands");
            foreach (var i in toWriteOut.Items)
                commands.Add(
                    new XElement("command", new XAttribute("name", "get_" + i.ID),
                             new XAttribute("protocmd", "getaddr"),
                        new XElement("shortDescription", i.Name),
                        new XElement("addr", i.Address.Substring(2)),
                        new XElement("blockLength", i.BlockLength),
                        new XElement("byteLength", i.ByteLength),
                        new XElement("bitPosition", i.BitPosition),
                        new XElement("bitLength", i.BitLength),
                        new XElement("description", i.Description)
                    ));

            XElement vito = new XElement("vito",
                new XElement("devices",
                    new XElement("device", new XAttribute("ID", "20CB"), new XAttribute("name", "VScotHO1"),
                                new XAttribute("protocol", "P300")
                    )
                ),
                commands);

            TextWriter writer2 = new StreamWriter("vito.xml");
            writer2.Write(vito);
            writer2.Close();



            Console.WriteLine("Fertig");
            Thread.Sleep(1000);
        }

        private static void fetchAdrFromStatistics()
        {
            var reader = new StreamReader(@"C:\Program Files\Viessmann Vitosoft 300 SID1\ServiceTool\MobileClient\log\statistic.log");
            string line = reader.ReadLine();
            while ((line = reader.ReadLine()) != null)
            {
                if (!line.Contains(";"))
                    continue;
                var parts = line.Split(';');
                if (parts.Length != 8)
                    continue;
                var part = parts[5];
                setOfAdrForFile.Add(part);
            }
        }
    }
}
