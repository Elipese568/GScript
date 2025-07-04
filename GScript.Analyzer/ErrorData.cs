using EUtility.ValueEx;

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
}
