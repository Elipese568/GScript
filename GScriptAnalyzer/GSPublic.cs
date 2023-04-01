using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScriptAnalyzer
{
    public class GSPublic
    {
        public static class GSErrorCode
        {
            public static readonly int GSE_ILLEGALCOMMAND = 1000;
            public static readonly int GSE_OUTOFLINERANGE = 1001;
            public static readonly int GSE_ARGUMENTOUTOFRANGE = 2001;
            public static readonly int GSE_WRONGARG = 1100;
        }

        public static int GetLastError()
        {
            return __M_Status.__error__;
        }

        public static void SetLastError(int errorcode)
        {
            __M_Status.__error__ = errorcode;
        }

        public static System.Exception GetException()
        {
            return __M_Status.__systemexception__ ?? new System.Exception("Do not have any exception");
        }

        public static void SetException(System.Exception exception)
        {
            __M_Status.__systemexception__ = exception;
        }
    }
}
