using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    public partial class EventValueType
    {
        public override string ToString()
        {
            if(DataType == "VarChar")
            {
                var sb = new StringBuilder();
                var name = Name.Trans();
                
                sb.Append($"{EnumAddressValue} -> {name.TrimStart('0','1', '2', '3', '4', '5', '6', '7', '8', '9').Trim()}");
                var repl = EnumReplaceValue.Trans();
                if (repl.ToLower() != name.ToLower() && !string.IsNullOrWhiteSpace(repl))
                    sb.Append($"/{repl}");

                var desc = Description.Trans();
                if (desc.ToLower() != name.ToLower() && desc.ToLower() != repl.ToLower() && !string.IsNullOrWhiteSpace(desc))
                    sb.Append($"/{desc}");

                var stat = StatusType?.Name.Trans();
                if (stat != "Undefined")
                    sb.Append($" -> {stat}");
                return sb.ToString();
            }

            return $"N:{Name.Trans()} V:{EnumAddressValue} S:{EnumReplaceValue.Trans()} U:{Unit.Trans()} Dt:{DataType} S:{Stepping} P:{ValuePrecision} R:{LowerBorder}-{UpperBorder} L:{Length} St:{StatusType?.Name.Trans()} D:{Description.Trans()}";
        }
    }

    public partial class DatapointTypeGroup
    {
        public override string ToString()
        {
            return $"{Name.Trans()}";
        }

    }

    public partial class DatapointType
    {
        public override string ToString()
        {
            var desc = Description?.Trans();
            desc = desc?.Substring((int)(desc?.IndexOf(':')+1));
            return $"{Id} {Address} {desc}";
        }

    }

    public partial class EventTypeGroup
    {
        public override string ToString()
        {
            return Name.Trans();
        }

    }

    public partial class EventType
    {
        public override string ToString()
        {
            return Name.Trans();
        }

    }
}
