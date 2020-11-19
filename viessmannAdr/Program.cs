using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using WpfApp1;

namespace viessmannAdr
{
    class Program
    {
        static Dictionary<string, string> textValues = new Dictionary<string, string>();
        static List<string> addessesForFile = new List<string>();
        static ViessmannEntities dc = new ViessmannEntities();
        static Properties.Settings config = Properties.Settings.Default;

        static void Main(string[] args)
        {
            try
            {
                Translator.InitTextValues();
                var allCount = dc.DatapointTypes.Count();
                int i = 0;
                Console.WriteLine(allCount);
                var lst = new List<XElement>();
                foreach (var dpt in dc.DatapointTypes.OrderBy(_ => _.Id))
                {

                    var groups = dpt.EventTypeGroups.Where(_ => _.ParentId == -1
                    && (_.EventTypeLinks.Any() || _.ChildEventTypeGroups.Any())
                    //&& _.DataPointTypeId == 350
                    ).OrderBy(_ => _.OrderIndex);

                    Console.WriteLine(i++);
                    foreach (var group in groups)
                    {

                        XElement element = MapGroup(group);

                        lst.Add(element);
                    }



                }

                var doc = new XDocument(new XElement("EventTypeGroups", lst));
                

                XmlTextWriter writer = new XmlTextWriter(@"ecnEventTypeGroup.xml", Encoding.UTF8);
                writer.Formatting = Formatting.Indented;
                doc.Save(writer);
                writer.Close();


                /*
                initTextValues();
                var device = chooseDevice();

                foreach (var g in selectGroups(device))
                {
                    printEventGroup(0, g, true);
                    //generateVitoXML(g);
                    //addessesForFile.Clear();
                }*/
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Fertig! Press any key!");
                Console.ReadKey();
            }
        }

        private static XElement MapGroup(EventTypeGroup group)
        {
            var element = new XElement("EventTypeGroup",
                        new XElement("Name", group.Name.Trans()),
                        new XElement("ID", group.Address),
                        new XElement("DataPointTypes",
                            new XElement("DataPointTypeID", group.DatapointType.Address)
                        ));

            if (group.EventTypeLinks.Any())
                element.Add(new XElement("EventTypes", MapEventTypes(group.EventTypeLinks.OrderBy(_ => _.EventTypeOrder).ToList().Select(_ => _.EventType))));

            if (group.DisplayConditionGroups.Any())
                element.Add(new XElement("DisplayConditionGroups", MapDisplayConditionGroups(group.DisplayConditionGroups)));

            foreach (var subgroup in group.ChildEventTypeGroups.Where(_ => _.EventTypeLinks.Any() || _.ChildEventTypeGroups.Any()))
                element.Add(MapGroup(subgroup));


            return element;
        }

        private static IEnumerable<XElement> MapDisplayConditionGroups(IEnumerable<DisplayConditionGroup> conditionGroups)
        {
            foreach (var group in conditionGroups)
            {
                yield return MapDisplayConditionGroup(group);
            }
        }

        private static XElement MapDisplayConditionGroup(DisplayConditionGroup group)
        {
            var element = new XElement("DisplayConditionGroup");
            if (group.Type != 1)
                element.Add(new XAttribute("Type", "And"));
            else
                element.Add(new XAttribute("Type", "Or"));

            if (group.ChildDisplayConditionGroups.Any())
                element.Add(new XElement("DisplayConditionGroups", MapDisplayConditionGroups(group.ChildDisplayConditionGroups)));

            if (group.DisplayConditions.Any())
                element.Add(new XElement("DisplayConditions", MapDisplayConditions(group.DisplayConditions)));

            return element;
        }

        private static IEnumerable<XElement> MapDisplayConditions(ICollection<DisplayCondition> displayConditions)
        {
            foreach (var condition in displayConditions)
            {
                var element = new XElement("DisplayCondition",
                    new XElement("EventTypeID", condition.EventTypeCondition.Address.Split('~')[0]),
                    new XElement("Value", condition.EventValueCondition.EnumAddressValue));

                if (condition.Condition == 1)
                    element.Add(new XAttribute("Type", "Equal"));
                else
                    element.Add(new XAttribute("Type", "NotEqual"));

                yield return element;
            }
        }

        private static IEnumerable<XElement> MapEventTypes(IEnumerable<EventType> eventTypes)
        {
            foreach (var eventType in eventTypes)
            {
                var element = new XElement("EventType", new XElement("EventTypeID", eventType.Address.Split('~')[0]));
                if (eventType.DisplayConditionGroups.Any())
                    element.Add(new XElement("DisplayConditionGroups", MapDisplayConditionGroups(eventType.DisplayConditionGroups)));
                yield return element;
            }
        }

        private static DatapointType chooseDevice()
        {
            var devQuery = dc.DatapointTypes.Where(_ => _.DataPoints.Any());
            var devList = devQuery.ToList();
            int idx = 0;
            if (devList.Count() > 1)
            {
                Console.WriteLine("Choose device:");
                foreach (var d in devList)
                {
                    Console.WriteLine(devList.IndexOf(d) + " " + trans(d.Name));
                }
                var choice = Console.ReadLine();
                idx = int.Parse(choice);
            }
            return devList[idx];
        }

        private static IEnumerable<EventTypeGroup> selectGroups(DatapointType device)
        {

            return device.EventTypeGroups.Where(_ => _.ParentId == -1 &&
                (_.ChildEventTypeGroups.Any() || _.EventTypeLinks.Any())).OrderBy(_ => _.OrderIndex);

        }

        private static void addIfNotThere(EventType evt)
        {
            if (!addessesForFile.Contains(evt.Address))
                addessesForFile.Add(evt.Address);
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

        public static string trans(string para)
        {
            if (string.IsNullOrWhiteSpace(para))
                return para;
            var ret = para.Trim('@');
            ret = ret.Replace("viessmann.eventvaluetype.name.", "viessmann.eventvaluetype.");
            if (textValues.ContainsKey(ret))
                ret = textValues[ret];

            return ret.Trim();
        }

        static void printEventGroup(int indent, EventTypeGroup g, bool print)
        {
            var name = trans(g.Name);
            bool showResult = true;

            var hideGroup = EvalConditions(indent, g.DisplayConditionGroups, false);
            if (hideGroup && showResult)
                return;

            if (print)
                Console.WriteLine(new string(' ', indent) + "G (" + g.Id + ") " + name + (hideGroup ? " (Hidden) " : " ") + g.OrderIndex);

            return;
            EvalConditions(indent + 8, g.DisplayConditionGroups, !showResult);

            foreach (var link in g.EventTypeLinks.OrderBy(_ => _.EventTypeOrder))
            {
                var evName = link.EventType.Name.Trim('@');
                if (textValues.ContainsKey(evName))
                    evName = textValues[evName];

                var hide = EvalConditions(indent + 16, link.EventType.DisplayConditionGroups, false);
                if (!hide || !showResult)
                {
                    if (print)
                        Console.WriteLine(new string(' ', indent + 8) + "E " + evName + (hide ? " (Hidden)" : ""));
                    EvalConditions(indent + 16, link.EventType.DisplayConditionGroups, !showResult);
                    addIfNotThere(link.EventType);
                    foreach (var val in link.EventType.EventValueTypes)
                        Console.Write(/*new string(' ', indent + 24) +*/  val.ToString());
                }
            }

            foreach (var sg in g.ChildEventTypeGroups.OrderBy(o => o.OrderIndex))
                printEventGroup(indent + 8, sg, print);
        }

        private static bool EvalConditions(int indent, ICollection<DisplayConditionGroup> g, bool debug)
        {
            bool ret = false;
            //foreach (var c in g)
            //{
            //    bool cond = false;
            //    foreach (var co in c.DisplayConditions)
            //    {
            //        cond = co.EventGroupValueCache.Where(cv=> cv.DataPointId == device.Id).Any() && co.EqualCondition;
            //        if (!co.EqualCondition)
            //            cond = !cond;

            //        if (cond)
            //            break;
            //    }

            //    ret = cond;
            //    if (!debug)
            //    {
            //        if (cond)
            //            break;
            //        else
            //            continue;
            //    }

            //    var name = trans(c.Name);
            //    Console.WriteLine(new string(' ', indent) + "C " + name + " (" + cond + ")");
            //    foreach (var co in c.ecnDisplayCondition)
            //    {
            //        var value = co.ecnEventGroupValueCache.Any();

            //        if (!co.EqualCondition)
            //            value = !value;

            //        Console.WriteLine(new string(' ', indent + 8) + trans(co.ecnEventType.Name) + " " +
            //            (co.EqualCondition ? "==" : "!=") + " " + trans(co.ecnEventValueType.Name) +
            //            " (" + value + ")");
            //    }
            //}
            return ret;
        }

        static void generateVitoXML(EventTypeGroup g)
        {

            //    Dictionary<string, EventTypesEventType> evTypes = new Dictionary<string, EventTypesEventType>();

            //    XmlSerializer evSerializer = new XmlSerializer(typeof(EventTypes));

            //    var evDict = new Dictionary<string, string>();
            //    var dc = new DataClasses1DataContext();
            //    var query = from t in dc.ecnDataPointTypeEventTypeLink
            //                select new { t.ecnEventType.Address, t.ecnEventType.Name };
            //    foreach (var item in query)
            //        evDict[item.Address] = trans(item.Name);

            //    var allEvTypes = new Dictionary<string, EventTypesEventType>();
            //    EventTypes types = (EventTypes)evSerializer.Deserialize(
            //        new XmlTextReader(Path.Combine(config.EventTypePath, "ecnEventType.xml")));
            //    foreach (var item in types.Items)
            //        allEvTypes[item.ID] = item;
            //    types = (EventTypes)evSerializer.Deserialize(
            //        new XmlTextReader(Path.Combine(config.EventTypePath, "sysDeviceIdent.xml")));
            //    foreach (var item in types.Items)
            //        allEvTypes[item.ID] = item;
            //    types = (EventTypes)evSerializer.Deserialize(
            //        new XmlTextReader(Path.Combine(config.EventTypePath, "sysDeviceIdentExt.xml")));
            //    foreach (var item in types.Items)
            //        allEvTypes[item.ID] = item;

            //    List<EventTypesEventType> wantedEvTypes = new List<EventTypesEventType>();
            //    int count = 0;
            //    foreach (var a in addessesForFile)
            //    {
            //        var t = allEvTypes[a];
            //        if (!evDict.ContainsKey(t.ID) && !(t.Name != null && t.Name.StartsWith("sys")) || string.IsNullOrEmpty(t.Address))
            //            continue;

            //        wantedEvTypes.Add(t);
            //        if (evDict.ContainsKey(t.ID))
            //            t.Name = evDict[t.ID];

            //        if (t.ID.Contains('~'))
            //        {
            //            t.ID = t.ID.Remove(t.ID.IndexOf('~'));
            //        }

            //        if (t.Description != null)
            //        {
            //            t.Description = trans(t.Description).Replace("##ecnnewline##", "\n").Replace("##ecntab##", "\t");
            //        }
            //        if (t.ValueList != null)
            //        {
            //            var builder = new StringBuilder();
            //            var values = t.ValueList.Split(';');
            //            foreach (var value in values)
            //            {
            //                var pair = value.Split('=');
            //                builder.Append(pair[0]);
            //                builder.Append("=");
            //                builder.Append(trans(pair[1]));
            //                builder.Append(";");
            //            }
            //            t.ValueList = builder.ToString().Trim(';');
            //        }
            //        if (t.Unit != null)
            //        {
            //            t.Unit = trans(t.Unit);
            //        }
            //        count++;
            //    }
            //    //foreach(var conv in wantedEvTypes.Select(ev => ev.Conversion).Distinct())
            //    //    Console.WriteLine(conv);

            //    Console.WriteLine("found " + count + " datapoints");
            //    EventTypes toWriteOut = new EventTypes();
            //    toWriteOut.Items = wantedEvTypes.OrderBy(t => t.Address).ToArray();


            //    //TextWriter writer = new StreamWriter("eventTypes.xml");
            //    //evSerializer.Serialize(writer, toWriteOut);
            //    //writer.Close();

            //    XElement commands = new XElement("commands");
            //    foreach (var i in toWriteOut.Items)
            //    {

            //       var cmd =  new XElement("command", new XAttribute("name", "get_" + i.ID),
            //                     new XAttribute("protocmd", "getaddr"),
            //                new XElement("addr", i.Address.Substring(2)),
            //                new XElement("parameter", i.Parameter),
            //                new XElement("conversion", i.Conversion)
            //            );

            //        if (i.ValueList != null)
            //            cmd.Add(new XElement("valueList", i.ValueList));

            //        if (i.ConversionFactor != null)
            //            cmd.Add(new XElement("conversionFactor", i.ConversionFactor));

            //        if (i.ConversionOffset != null)
            //            cmd.Add(new XElement("conversionOffset", i.ConversionOffset));

            //        if (i.Description != null)
            //            cmd.Add(new XElement("description", i.Description));

            //        if (i.BitLength != null)
            //            cmd.Add(new XElement("bitLength", i.BitLength));

            //        if (i.BitPosition != null)
            //            cmd.Add(new XElement("bitPosition", i.BitPosition));

            //        if (i.Name != null)
            //            cmd.Add(new XElement("shortDescription", i.Name));

            //        if (i.BytePosition != null)
            //            cmd.Add(new XElement("bytePosition", i.BytePosition));

            //        if (i.BlockLength != null)
            //            cmd.Add(new XElement("blockLength", i.BlockLength));

            //        if (i.ByteLength != null)
            //            cmd.Add(new XElement("byteLength", i.ByteLength));

            //        commands.Add(cmd);
            //    }

            //    XElement vito = new XElement("vito",
            //        new XElement("devices",
            //            new XElement("device", new XAttribute("ID", "20CB"), new XAttribute("name", "VScotHO1"),
            //                        new XAttribute("protocol", "P300")
            //            )
            //        ),
            //        commands);

            //    var fileName = trans(g.Name) + "_vito.xml";
            //    TextWriter writer2 = new StreamWriter(fileName);
            //    writer2.Write(vito);
            //    writer2.Close();
            //    Console.WriteLine("Written to " + fileName + "\n");
        }
    }
}
