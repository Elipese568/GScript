using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScript.Analyzer
{
    public class Flag : ScriptObject
    {
        public string FlagName { get; set; }
        public string FlagValue { get; set; }

        public Flag(string flagname, string flagvalue) : base()
        {
            FlagName = flagname;
            FlagValue = flagvalue;
            Value = this;
        }

        public override string ToString()
        {
            return FlagName;
        }

        public override string ToStringDescription()
        {
            return $"{{FlagName : \"{FlagName}\", FlagValue : \"{FlagValue}\"";
        }
    }
}
