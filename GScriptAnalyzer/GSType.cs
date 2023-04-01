using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScriptAnalyzer
{
    public class GSType : GSObject
    {
        public GSType(Type type) : base()
        {
            Value = type;
        }
    }
}
