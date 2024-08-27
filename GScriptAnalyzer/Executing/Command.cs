// AT:   Elipese
// Date: 2023/2/12

using EUtility.ValueEx;
using GScript.Analyzer.Exception;
using GScript.Analyzer.InternalType;
using GScript.Analyzer.Parser;
using GScript.Analyzer.Util;
using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace GScript.Analyzer.Executing;

// Date: 2023/2/12
// Rename: 2024/2/15
public class Command
{
    private string m_name;
    private List<ScriptObject> m_args = new List<ScriptObject>();
    private Dictionary<ScriptObject, ParenthesisType> m_typeargpairs = new Dictionary<ScriptObject, ParenthesisType>();
    private ParserBase m_commandParser = ParserBase.Empty;

    public string Name { get => m_name; private set => m_name = value; }
    public List<ScriptObject> Args { get => m_args; private set => m_args = value; }
    public Dictionary<ScriptObject, ParenthesisType> TypeArgPairs { get => m_typeargpairs; private set => m_typeargpairs = value; }
    public ParserBase Parser { get => m_commandParser; private set => m_commandParser = value; }


    public static readonly Dictionary<string, ObjectType> KnownTypes = new()
    {
        ["number"] = new(typeof(long)),
        ["long_number"] = new(typeof(Int128)),
        ["larger_number"] = new(typeof(BigInteger)),
        ["string"] = new(typeof(string)),
        ["char"] = new(typeof(char)),
        ["float"] = new(typeof(double)),
        ["bool"] = new(typeof(bool)),
        ["type"] = new(typeof(ObjectType)),
        ["any"] = new(typeof(object))
    };

    public Command(string com, ParserBase parser)
    {
        parser.Parse(com);

        Name = parser.ComplateParserTokens.Find(x => x.TokenMetaData.Type == ParserTokenType.Command).TokenData.Value as string;

        Args = new(parser.ComplateParserTokens.ToArray()[1..].Select(x => x.TokenData));

        var cct = parser.ComplateParserTokens;
        cct.RemoveAll(x => x.TokenMetaData.Type == ParserTokenType.Command);

        var varSwitch =
            VariableSwitch<Union<string, Type>>
                .Create()
                .CreateCase(typeof(Variable), x =>
                {
                    return ParenthesisType.Small;
                })
                .CreateCase(typeof(object), x =>
                {
                    return ParenthesisType.Small;
                })
                .CreateCase("Literal", x =>
                {
                    return ParenthesisType.None;
                })
                .CreateCase(typeof(ObjectType), x =>
                {
                    return ParenthesisType.Big;
                })
                .SetDefault(x =>
                {
                    return ParenthesisType.Middle;
                });

        TypeArgPairs = cct.ToDictionary(
        k => k.TokenData,
        v => v.TokenMetaData.Type switch
        {
            ParserTokenType.Literal => ParenthesisType.None,
            ParserTokenType.Argument => (ParenthesisType)varSwitch.Switch(v.TokenData.ValueType)
        });

        if (Args.Exists(x => x is Variable))
            Script.CurrentScript.RegisterGlobalCommandHandler(__GlobalHandler);
    }

    private void __GlobalHandler(Command cmd, ref bool cancel, ref int line)
    {
        int i = 0;
        ScriptObject[] ArgT = new ScriptObject[Args.Count];
        Args.CopyTo(ArgT);

        foreach (var arg in ArgT)
        {
            i++;
            if (arg.GetType() != typeof(Variable))
                continue;

            try
            {
                Args[i - 1] = Script.CurrentScript.Vars[(arg as Variable).Name];
            }
            catch { }
            var k = Args[i - 1];
            TypeArgPairs.Remove(k);
            TypeArgPairs.Add(Args[i - 1], ParenthesisType.Small);
        }
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("{\"CommandHead\":\"");
        sb.Append(m_name);
        sb.Append("\",\"CommandArg\":[");
        sb.Append(m_args[0]);
        foreach (ScriptObject gso in m_args)
        {
            sb.Append(',');
            sb.Append(gso.ToStringDescription());
        }
        sb.Append("]}");

        return sb.ToString();
    }

    public string ToCommandString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append(m_name);
        sb.Append(' ');
        foreach (var arg in m_typeargpairs)
        {
            sb.Append(StrParenthesis.GetParenthesisTypeString(arg.Value, arg.Value == ParenthesisType.Small ? ((Variable)arg.Key).Name : arg.Key.Value.ToString()));
            sb.Append(' ');
        }
        return sb.ToString().TrimEnd(' ');
    }
}
