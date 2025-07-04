using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GScript.Analyzer.__Internal;

namespace GScript.Analyzer
{
    public class ExceptionOperator
    {
        public static class GErrorCode
        {
            public static readonly int GSE_ILLEGALCOMMAND = 1000;
            public static readonly int GSE_OUTOFLINERANGE = 1001;
            public static readonly int GSE_ARGUMENTOUTOFRANGE = 2001;
            public static readonly int GSE_WRONGARG = 1100;
            public static readonly int GSE_OVERFLOW = 8000;
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

        public static void SetLastErrorEx(ErrorData errorData)
        {
            __M_Status.__errorData__ = errorData;
        }

        public static ErrorData GetErrorData()
        {
            return __M_Status.__errorData__ ?? new ErrorData(-1,"",-1,"Do not have any execption.");
        }
    }
}
