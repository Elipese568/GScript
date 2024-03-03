// AT:   Elipese
// Date: 2023/2/12

using GScript.Analyzer.Exception;
using GScript.Analyzer.Util;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace GScript.Analyzer;

// Date: 2023/2/12
// Rename: 2024/2/15
public class Command
{
    private string m_name;
    private List<ScriptObject> m_args = new List<ScriptObject>();
    private Dictionary<ScriptObject, ParenthesisType> m_typeargpairs = new Dictionary<ScriptObject, ParenthesisType>();

    public string Name { get => m_name; set => m_name = value; }
    public List<ScriptObject> Args { get => m_args; set => m_args = value; }
    public Dictionary<ScriptObject, ParenthesisType> TypeArgPairs { get => m_typeargpairs; set => m_typeargpairs = value; }

    public static readonly Dictionary<string, ObjectType> KnownTypes = new()
    {
        ["number"] = new(typeof(long)),
        ["long_number"] = new(typeof(Int128)),
        ["larger_number"] = new(typeof(BigInteger)),
        ["string"] = new(typeof(string)),
        ["char"] = new(typeof(Char)),
        ["float"] = new(typeof(double)),
        ["bool"] = new(typeof(bool)),
        ["type"] = new(typeof(ObjectType)),
        ["any"] = new(typeof(object))
    };

    public Command() : this("") { }

    public Command(string com) : this(new StringSplit(com,' ')) { }

    Command(StringSplit split)
    {
        Script.CurrentScript.RegisterGlobalCommandHandler(__GlobalHandler);

        m_name = split[0];
        foreach(string su in split[1..])
        {
            ScriptObject go = new();
            var part = StrParenthesis.GetStringParenthesisType(su);
            switch(part)
            {
                case ParenthesisType.Unknown:
                    throw new ArgFormatException("参数括号未知");
                case ParenthesisType.Big:
                    if (KnownTypes.ContainsKey(su[1..(su.Length - 1)]))
                        go = KnownTypes[su[1..(su.Length - 1)]];
                    else
                    {
                        Type t = System.Type.GetType(su[1..(su.Length - 1)].Replace("::", "System.").Replace(".::", "GScript.Standard"));
                        if (t == null)
                            go = new ObjectType(su[1..(su.Length - 1)]);
                        else
                            go = new ObjectType(t);
                    }
                    break;
                case ParenthesisType.Middle:
                    go = new ScriptObject();
                    var sp = new StringSplit(su[1..(su.Length - 1)], ':');
                    switch (sp[0])
                    {
                        case "string":
                            go.Value = sp[1];
                            break;
                        case "number":
                            go.Value = Convert.ToInt64(sp[1]);
                            break;
                        case "long_number":
                            go.Value = Int128.Parse(sp[1]);
                            break;
                        case "larger_number":
                            go.Value = BigInteger.Parse(sp[1]);
                            break;
                        case "char":
                            if (sp[1].Length != 1)
                            {
                                ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_WRONGARG);
                                ExceptionOperator.SetException(new ArgumentOutOfRangeException("Type [char] cannot to mul. chars."));
                                throw new ArgFormatException("Type [char] cannot to mul. chars.");
                            }
                            go.Value = sp[1][0];
                            break;
                        case "bool":
                            go.Value = Boolean.Parse(sp[1]);
                            break;
                        case "flag":
                            var sp2 = new StringSplitEx(string.Join(':', sp.SplitUnit), ':', 3);
                            go = new Flag(sp2[1], sp2[2]);
                            break;
                        case "tag_t":
                            go = new Tag(sp[1]);
                            break;
                        default:
                            go = new ScriptObject()
                            {
                                Value = sp[1],
                                ValueType = sp[0]
                            };
                            break;
                    }
                    break;
                case ParenthesisType.Small:
                    if(Script.CurrentScript.Vars.ContainsKey(su[1..(su.Length - 1)]))
                    {
                        go = Script.CurrentScript.Vars[su[1..(su.Length - 1)]];
                    }
                    else
                    {
                        go = new Variable() { Name = su[1..(su.Length - 1)] };
                        //GSScript.CurrentScript.Vars.Add(su[1..(su.Length - 2)], new GSVar() { Name = su[1..(su.Length - 2)] });
                    }
                    break;
            }
            m_args.Add(go);

            m_typeargpairs.Add(go, part);
        }
    }

    private void __GlobalHandler(Command cmd, ref bool cancel, ref int line)
    {
        int i = 0;
        ScriptObject[] ArgT = new ScriptObject[Args.Count];
        Args.CopyTo(ArgT);

        foreach(var arg in ArgT)
        {
            i++;
            if (arg.GetType() != typeof(Variable))
                continue;

            try
            {
                Args[i - 1] = Script.CurrentScript.Vars[(arg as Variable).Name];
            }
            catch { }
            var k = TypeArgPairs.ToList()[i - 1].Key;
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
        foreach(ScriptObject gso in m_args)
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
        foreach(var arg in m_typeargpairs)
        {
            sb.Append(StrParenthesis.GetParenthesisTypeString(arg.Value, (arg.Value == ParenthesisType.Small) ? ((Variable)arg.Key).Name : arg.Key.Value.ToString()));
            sb.Append(' ');
        }
        return sb.ToString().TrimEnd(' ');
    }
}
