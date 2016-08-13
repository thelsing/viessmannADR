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
        static List<string> addessesForFile = new List<string>();
        static DataClasses1DataContext dc = new DataClasses1DataContext();
        static Properties.Settings config = Properties.Settings.Default;
        static ecnDataPoint device;

        static void Main(string[] args)
        {
            initTextValues();
            chooseDevice();

            foreach (var g in selectGroups())
            {
                fetchAdrFromEventGroup(g.Id);
                //fetchAdrFromEventGroup(0);
                generateVitoXML(g);
                addessesForFile.Clear();
            }
            Console.WriteLine("All Done. Press any key to close.");
            Console.ReadKey();
        }

        private static void chooseDevice()
        {
            var devQuery = from d in dc.ecnDataPoint
                           where d.Id != 1
                           select d;
            var devList = devQuery.ToList();
            int idx = 0;
            if (devList.Count() > 1)
            {
                Console.WriteLine("Choose device:");
                foreach (var d in devList)
                {
                    Console.WriteLine(devList.IndexOf(d) + " " + d.Name);
                }
                var choice = Console.ReadLine();
                idx = int.Parse(choice);
            }
            device = devList[idx];
        }

        private static IQueryable<ecnEventTypeGroup> selectGroups()
        {
            var query = from g in dc.ecnEventTypeGroup
                        where g.DataPointTypeId == device.DataPointTypeId
                        && g.ParentId == -1
                        && (g.ecnEventTypeGroup2.Any() || g.ecnEventTypeEventTypeGroupLink.Any())
                        select g;
            return query;
        }

        private static void addIfNotThere(ecnEventType evt)
        {
            if (!addessesForFile.Contains(evt.Address))
                addessesForFile.Add(evt.Address);
        }

        private static void fetchAdrFromEventGroup(int id)
        {
            var query = from g in dc.ecnEventTypeGroup
                        where g.DataPointTypeId == 350
                        && g.ParentId == -1
                        && (g.ecnEventTypeGroup2.Any() || g.ecnEventTypeEventTypeGroupLink.Any())
                        select g;

            if (id != 0)
                query = from g in dc.ecnEventTypeGroup
                        where g.Id == id
                        select g;

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
            XmlSerializer trSerializer = new XmlSerializer(typeof(DocumentElement));

            DocumentElement de = (DocumentElement)trSerializer.Deserialize(
                new XmlTextReader(Path.Combine(config.TextRessourcePath, "Textresource_" + config.OutputLang + ".xml")));
            foreach (var o in de.Items)
            {
                var trs = o as DocumentElementTextResources;
                if (trs == null)
                    continue;

                foreach (var item in trs.TextResource)
                    textValues[item.Label] = item.Value;

                break;
            }
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

            if (print)
                Console.WriteLine(new string(' ', indent) + "G (" + g.Id + ") " + name + (hideGroup ? " (Hidden) " : " ") + g.OrderIndex);
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
                    if (print)
                        Console.WriteLine(new string(' ', indent + 8) + "E " + evName + (hide ? " (Hidden)" : ""));
                    EvalConditions(indent + 16, evl.ecnEventType.ecnDisplayConditionGroup, !showResult);
                    addIfNotThere(evl.ecnEventType);
                }
            }

            g.ecnEventTypeGroup2.Load();
            foreach (var sg in g.ecnEventTypeGroup2.OrderBy(o => o.OrderIndex))
                printEventGroup(indent + 8, sg, print);
        }

        private static bool EvalConditions(int indent, EntitySet<ecnDisplayConditionGroup> g, bool debug)
        {
            bool ret = false;
            foreach (var c in g)
            {
                c.ecnDisplayCondition.Load();
                bool cond = false;
                foreach (var co in c.ecnDisplayCondition)
                {
                    cond = co.ecnEventGroupValueCache.Where(cv=> cv.DataPointId == device.Id).Any() && co.EqualCondition;
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

        static void generateVitoXML(ecnEventTypeGroup g)
        {

            Dictionary<string, EventTypesEventType> evTypes = new Dictionary<string, EventTypesEventType>();

            XmlSerializer evSerializer = new XmlSerializer(typeof(EventTypes));

            var evDict = new Dictionary<string, string>();
            var dc = new DataClasses1DataContext();
            var query = from t in dc.ecnDataPointTypeEventTypeLink
                        select new { t.ecnEventType.Address, t.ecnEventType.Name };
            foreach (var item in query)
                evDict[item.Address] = trans(item.Name);

            var allEvTypes = new Dictionary<string, EventTypesEventType>();
            EventTypes types = (EventTypes)evSerializer.Deserialize(
                new XmlTextReader(Path.Combine(config.EventTypePath, "ecnEventType.xml")));
            foreach (var item in types.Items)
                allEvTypes[item.ID] = item;
            types = (EventTypes)evSerializer.Deserialize(
                new XmlTextReader(Path.Combine(config.EventTypePath, "sysDeviceIdent.xml")));
            foreach (var item in types.Items)
                allEvTypes[item.ID] = item;
            types = (EventTypes)evSerializer.Deserialize(
                new XmlTextReader(Path.Combine(config.EventTypePath, "sysDeviceIdentExt.xml")));
            foreach (var item in types.Items)
                allEvTypes[item.ID] = item;

            List<EventTypesEventType> wantedEvTypes = new List<EventTypesEventType>();
            int count = 0;
            foreach (var a in addessesForFile)
            {
                var t = allEvTypes[a];
                if (!evDict.ContainsKey(t.ID) && !(t.Name != null && t.Name.StartsWith("sys")) || string.IsNullOrEmpty(t.Address))
                    continue;

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
            //foreach(var conv in wantedEvTypes.Select(ev => ev.Conversion).Distinct())
            //    Console.WriteLine(conv);

            Console.WriteLine("found " + count + " datapoints");
            EventTypes toWriteOut = new EventTypes();
            toWriteOut.Items = wantedEvTypes.OrderBy(t => t.Address).ToArray();


            //TextWriter writer = new StreamWriter("eventTypes.xml");
            //evSerializer.Serialize(writer, toWriteOut);
            //writer.Close();

            XElement commands = new XElement("commands");
            foreach (var i in toWriteOut.Items)
            {
                
               var cmd =  new XElement("command", new XAttribute("name", "get_" + i.ID),
                             new XAttribute("protocmd", "getaddr"),
                        new XElement("addr", i.Address.Substring(2)),
                        new XElement("parameter", i.Parameter),
                        new XElement("conversion", i.Conversion)
                    );

                if (i.ValueList != null)
                    cmd.Add(new XElement("valueList", i.ValueList));

                if (i.ConversionFactor != null)
                    cmd.Add(new XElement("conversionFactor", i.ConversionFactor));

                if (i.ConversionOffset != null)
                    cmd.Add(new XElement("conversionOffset", i.ConversionOffset));

                if (i.Description != null)
                    cmd.Add(new XElement("description", i.Description));

                if (i.BitLength != null)
                    cmd.Add(new XElement("bitLength", i.BitLength));

                if (i.BitPosition != null)
                    cmd.Add(new XElement("bitPosition", i.BitPosition));

                if (i.Name != null)
                    cmd.Add(new XElement("shortDescription", i.Name));

                if (i.BytePosition != null)
                    cmd.Add(new XElement("bytePosition", i.BytePosition));

                if (i.BlockLength != null)
                    cmd.Add(new XElement("blockLength", i.BlockLength));

                if (i.ByteLength != null)
                    cmd.Add(new XElement("byteLength", i.ByteLength));

                commands.Add(cmd);
            }

            XElement vito = new XElement("vito",
                new XElement("devices",
                    new XElement("device", new XAttribute("ID", "20CB"), new XAttribute("name", "VScotHO1"),
                                new XAttribute("protocol", "P300")
                    )
                ),
                commands);

            var fileName = trans(g.Name) + "_vito.xml";
            TextWriter writer2 = new StreamWriter(fileName);
            writer2.Write(vito);
            writer2.Close();
            Console.WriteLine("Written to " + fileName + "\n");
        }
    }
}
