using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace WpfApp1
{
    public static class Translator
    {
        private static Dictionary<string, string> _textValues = new Dictionary<string, string>();
        public static void InitTextValues()
        {
            var config = viessmannAdr.Properties.Settings.Default;
            XmlSerializer trSerializer = new XmlSerializer(typeof(DocumentElement));

            DocumentElement de = (DocumentElement)trSerializer.Deserialize(
                new XmlTextReader(Path.Combine(config.TextRessourcePath, "Textresource_" + config.OutputLang + ".xml")));
            foreach (var o in de.Items)
            {
                var trs = o as DocumentElementTextResources;
                if (trs == null)
                    continue;

                foreach (var item in trs.TextResource)
                    _textValues[item.Label] = item.Value;

                break;
            }
        }

        public static string Trans(this string para)
        {
            if (string.IsNullOrWhiteSpace(para))
                return para;
            var ret = para.Trim('@');
            ret = ret.Replace("viessmann.eventvaluetype.name.", "viessmann.eventvaluetype.");
            if (_textValues.ContainsKey(ret))
                ret = _textValues[ret];

            return ret.Trim();
        }
    }
}
