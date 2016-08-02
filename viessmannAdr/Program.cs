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

        static void Main(string[] args)
        {
            initTextValues();

            var dc = new DataClasses1DataContext();

            var query = from g in dc.ecnEventTypeGroup
                        where g.DataPointTypeId == 350
                        && g.ParentId == -1 
                        && (g.ecnEventTypeGroup2.Any() || g.ecnEventTypeEventTypeGroupLink.Any())
                        select g;

            foreach (var g in query)
            {
                printEventGroup(0, g);
                Console.WriteLine();
                Console.ReadKey();
            }
            Console.ReadKey();
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

        static void printEventGroup(int indent, ecnEventTypeGroup g)
        {
            var name = trans(g.Name);
            bool showResult = true;

            g.ecnDisplayConditionGroup.Load();
            var hideGroup = EvalConditions(indent, g.ecnDisplayConditionGroup, false);
            if (hideGroup && showResult)
                return;

            Console.WriteLine(new string(' ', indent) + "G " + name + (hideGroup? " (Hidden) ":" ") + g.OrderIndex);
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
                    Console.WriteLine(new string(' ', indent + 8) + "E " + evName + (hide ? " (Hidden)" : ""));
                    EvalConditions(indent + 16, evl.ecnEventType.ecnDisplayConditionGroup, !showResult);
                }
            }

            g.ecnEventTypeGroup2.Load();
            foreach (var sg in g.ecnEventTypeGroup2.OrderBy(o => o.OrderIndex))
                printEventGroup(indent + 8, sg);
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
            HashSet<string> set = new HashSet<string>();
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
                set.Add(part);
            }

            Dictionary<string, EventTypesEventType> evTypes = new Dictionary<string, EventTypesEventType>();
            Dictionary<string, string> textValues = new Dictionary<string, string>();

            XmlSerializer evSerializer = new XmlSerializer(typeof(EventTypes));
            XmlSerializer trSerializer = new XmlSerializer(typeof(TextResources));

            TextResources tr = (TextResources)trSerializer.Deserialize(new XmlTextReader(@"C:\Program Files\Viessmann Vitosoft 300 SID1\ServiceTool\Web\XmlDocuments\Textresource_de_tk.xml"));
            foreach (var item in tr.Items)
                textValues[item.Label] = item.Value;


            var evDict = new Dictionary<string, string>();
            var dc = new DataClasses1DataContext();
            var query = from t in dc.ecnDataPointTypeEventTypeLink
                        where t.DataPointTypeId == 350
                        select new { t.ecnEventType.Address, t.ecnEventType.Name};
            foreach (var item in query)
            {
                var namekey = item.Name.Trim('@');
                if (textValues.ContainsKey(namekey))
                    evDict[item.Address] = textValues[item.Name.Trim('@')];
                else
                    evDict[item.Address] = namekey;
            }

            List<EventTypesEventType> allEvTypes = new List<EventTypesEventType>();
            EventTypes types = (EventTypes)evSerializer.Deserialize(new XmlTextReader(@"C:\Program Files\Viessmann Vitosoft 300 SID1\ServiceTool\MobileClient\Config\ecnEventType.xml"));
            allEvTypes.AddRange(types.Items);
            types = (EventTypes)evSerializer.Deserialize(new XmlTextReader(@"C:\Program Files\Viessmann Vitosoft 300 SID1\ServiceTool\MobileClient\Config\sysDeviceIdent.xml"));
            allEvTypes.AddRange(types.Items);
            types = (EventTypes)evSerializer.Deserialize(new XmlTextReader(@"C:\Program Files\Viessmann Vitosoft 300 SID1\ServiceTool\MobileClient\Config\sysDeviceIdentExt.xml"));
            allEvTypes.AddRange(types.Items);

            List<EventTypesEventType> wantedEvTypes = new List<EventTypesEventType>();
            int count = 0;
            foreach (var t in allEvTypes)
            {
                if (!evDict.ContainsKey(t.ID) && !(t.Name != null && t.Name.StartsWith("sys")) || string.IsNullOrEmpty(t.Address))
                    continue;

                if (!set.Contains(t.ID))
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
                    var descKey = t.Description.Trim('@');
                    if (textValues.ContainsKey(descKey))
                        t.Description=textValues[descKey].Replace("##ecnnewline##", "\n").Replace("##ecntab##", "\t");
                }
                if (t.ValueList != null)
                {
                    var builder = new StringBuilder();
                    var values = t.ValueList.Split(';');
                    foreach(var value in values)
                    {
                        var pair = value.Split('=');
                        builder.Append(pair[0]);
                        builder.Append("=");
                        var descKey = pair[1].Trim('@');
                        if (textValues.ContainsKey(descKey))
                            builder.Append(textValues[descKey]);
                        else
                            builder.Append(descKey);
                        builder.Append(";");
                    }
                    t.ValueList = builder.ToString().Trim(';');
                }
                if (t.Unit != null)
                {
                    if (textValues.ContainsKey(t.Unit))
                        t.Unit = textValues[t.Unit];
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
    }
}
