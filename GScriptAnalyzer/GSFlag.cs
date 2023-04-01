using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScriptAnalyzer
{
    public class GSFlag : GSObject
    {
        public string FlagName { get; set; }
        public string FlagValue { get; set; }

        public GSFlag(string flagname, string flagvalue) : base()
        {
            FlagName = flagname;
            FlagValue = flagvalue;
        }

        public override string ToString()
        {
            return $"{{FlagName : \"{FlagName}\", FlagValue : \"{FlagValue}\"";
        }
    }
}
