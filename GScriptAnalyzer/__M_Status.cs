using EUtility.ValueEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScript.Analyzer
{
    public struct ErrorData
    {
        public int Line { get; set; }
        public string RawString { get; set; }

        public Union<int, System.Exception> InnerErrorData { get; set; }
        public string Message { get; set; }

        public ErrorData(int line, string rawstring, Union<int, System.Exception> innererrordata, string message)
        {
            Line = line;
            RawString = rawstring;
            InnerErrorData = innererrordata;
            Message = message;
        }
    }

    internal class __M_Status
    {
        internal static int __error__ = 0;
        internal static ErrorData? __errorData__ = null;
        internal static System.Exception __systemexception__ = null;
    }
}
