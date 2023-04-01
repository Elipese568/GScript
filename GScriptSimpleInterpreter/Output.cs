using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScriptSimpleInterpreter.Commands
{
    internal class Output : GSCmdBase
    {
        public override object Execute()
        {
            Console.WriteLine(Args[0].GetConvertValue<string>());
            return true;
        }
    }
}
