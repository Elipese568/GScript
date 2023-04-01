// At:   Elipese
// Date: 2023/2/12

namespace GScriptAnalyzer.Util
{
    // Date: 2023/2/12
    public enum ParenthesisType
    {
        Unknown = -1,
        Big, //    {}
        Middle, // []
        Small//    ()
    }

    // Date: 2023/2/12
    public class StrParenthesis
    {
        public static ParenthesisType GetStringParenthesisType(string str)
        {
            return
            (str[0].ToString() + str[str.Length-1].ToString()) switch
            {
                "{}" => ParenthesisType.Big,
                "[]" => ParenthesisType.Middle,
                "()" => ParenthesisType.Small,
                _ => ParenthesisType.Unknown
            } ;
        }

        public static string GetParenthesisTypeString(ParenthesisType pt, string str)
        {
            return
            pt switch
            {
                ParenthesisType.Small => $"({str})",
                ParenthesisType.Middle => $"[{str}]",
                ParenthesisType.Big => $"{{{str}}}",
                _ => str
            };
        }
    }
}
