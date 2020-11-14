using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace viessmannAdr
{
    public partial class EventValueType
    {
        public override string ToString()
        {
            if(DataType == "VarChar")
            {
                var sb = new StringBuilder();
                var name = Program.trans(Name);
                
                sb.Append($"{EnumAddressValue} -> {name.TrimStart('0','1', '2', '3', '4', '5', '6', '7', '8', '9').Trim()}");
                var repl = Program.trans(EnumReplaceValue);
                if (repl.ToLower() != name.ToLower() && !string.IsNullOrWhiteSpace(repl))
                    sb.Append($"/{repl}");

                var desc = Program.trans(Description);
                if (desc.ToLower() != name.ToLower() && desc.ToLower() != repl.ToLower() && !string.IsNullOrWhiteSpace(desc))
                    sb.Append($"/{desc}");

                var stat = Program.trans(StatusType?.Name);
                if (stat != "Undefined")
                    sb.Append($" -> {stat}");
                return sb.AppendLine().ToString();
            }

            return "";// $"N:{Program.trans(Name)} V:{EnumAddressValue} S:{Program.trans(EnumReplaceValue)} U:{Program.trans(Unit)} Dt:{DataType} S:{Stepping} P:{ValuePrecision} R:{LowerBorder}-{UpperBorder} L:{Length} St:{Program.trans(StatusType?.Name)} D:{Program.trans(Description)}";
        }
    }
}
