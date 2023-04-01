using GScriptAnalyzer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScriptSimpleInterpreter
{
    internal abstract class GSCmdBase
    {
        public GSCmdBase() : base()
        {
        }

        public abstract object Execute();
    }
}
