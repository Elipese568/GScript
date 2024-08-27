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
    None,
    Big, //    {}
    Middle, // []
    Small,//   ()
    Sharp,//   <>
    Half =   0b1000,
    Left =  0b10000,
    Right = 0b11000
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
                "<" or ">" => ParenthesisType.Sharp | ParenthesisType.Half,
                _ => ParenthesisType.Unknown,
            };
        }
        return
        (str[0].ToString() + str[str.Length-1].ToString()) switch
        {
            "{}" => ParenthesisType.Big,
            "[]" => ParenthesisType.Middle,
            "()" => ParenthesisType.Small,
            "<>" => ParenthesisType.Sharp,
            _ => ParenthesisType.Unknown
        } ;
    }

    public static ParenthesisType GetCharHalfParenthesisType(char c)
    {
        return
        c switch
        {
            '<' => ParenthesisType.Sharp | ParenthesisType.Left,
            '>' => ParenthesisType.Sharp | ParenthesisType.Right,
            '[' => ParenthesisType.Middle | ParenthesisType.Left,
            ']' => ParenthesisType.Middle | ParenthesisType.Right,
            '{' => ParenthesisType.Big | ParenthesisType.Left,
            '}' => ParenthesisType.Big | ParenthesisType.Right,
            '(' => ParenthesisType.Small | ParenthesisType.Left,
            ')' => ParenthesisType.Small | ParenthesisType.Right,
            _ => ParenthesisType.Unknown
        };
    }

    public static string GetParenthesisTypeString(ParenthesisType pt, string str)
    {
        return
        pt switch
        {
            ParenthesisType.Small => $"({str})",
            ParenthesisType.Middle => $"[{str}]",
            ParenthesisType.Big => $"{{{str}}}",
            ParenthesisType.Sharp => $"<{str}>",
            _ => str
        };
    }
}
