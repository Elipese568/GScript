// At:   Elipese
// Date: 2023/2/12

namespace GScript.Analyzer.Util;

// First Date: 2023/2/12

// Last Date: 2024/2/6
// Flags Attribute is because is give editer to native color.
[Flags]
public enum ParenthesisType
{
    Unknown = -1,
    Big, //    {}
    Middle, // []
    Small,//    ()
    Half = 0b100
}

// First Date: 2023/2/12
// Last Date: 2024/2/12
public class StrParenthesis
{
    public static ParenthesisType GetStringParenthesisType(string str)
    {
        if (str == "")
            return ParenthesisType.Unknown;
        if(str.Length == 1)
        {
            return
            str switch
            {
                "{" or "}" => ParenthesisType.Big | ParenthesisType.Half,
                "[" or "]" => ParenthesisType.Middle | ParenthesisType.Half,
                "(" or ")" => ParenthesisType.Small | ParenthesisType.Half,
                _ => ParenthesisType.Unknown,
            };
        }
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
