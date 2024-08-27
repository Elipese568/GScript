using EUtility.ValueEx;
using GScript.Analyzer.Exception;
using GScript.Analyzer.InternalType;
using GScript.Analyzer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace GScript.Analyzer.Parser;

public class DefaultCommandParser : ParserBase
{
    private List<Token> m_complateTokens = new List<Token>();
    private bool m_ignoreUnknownUnit = false;
    private static Dictionary<string, Converter<ScriptObject[], ScriptObject>> m_constantConverter = new();

    public DefaultCommandParser(string command, bool ignoreUnknownUnit = false) : base(command)
    {
        m_ignoreUnknownUnit = ignoreUnknownUnit;
    }

    public override List<Token> ComplateParserTokens => m_complateTokens;
    public bool IgnoreUnknownUnit => m_ignoreUnknownUnit;

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

    public static void RegisterCustomTypeConverter(string typename, Converter<ScriptObject[], ScriptObject?> converter)
    {
        m_constantConverter.Add(typename, converter);
    }

    public override bool Parse(string line)
    {
        m_complateTokens.Clear();

        StringSplit split = new(line, ' ');

        m_complateTokens.Add(Token.MakeToken(split[0], ParserTokenMetaData.MakeTokenData(ParserTokenType.Command, " "), new()
        {
            Value = split[0],
            ValueType = "Command"
        }));

        foreach (string su in split[1..])
        {
            ParserTokenType type;
            ScriptObject? go = ParseArgument(su, out type);
            if (go == null)
                return false;
            Token token = Token.MakeToken(su, ParserTokenMetaData.MakeTokenData(type, " "), go);
            m_complateTokens.Add(token);
        }

        return true;
    }

    public ScriptObject? ParseArgument(string splitUnit, out ParserTokenType type)
    {
        ScriptObject go = new();
        var part = StrParenthesis.GetStringParenthesisType(splitUnit);
        string parContent = splitUnit[1..(splitUnit.Length - 1)];
        switch (part)
        {
            case ParenthesisType.Unknown:
                if (IgnoreUnknownUnit)
                {
                    go.Value = splitUnit;
                    go.ValueType = "Literal";
                    type = ParserTokenType.Literal;
                    break;
                }
                throw new ArgFormatException("参数括号未知");
            case ParenthesisType.Big:
                if (KnownTypes.TryGetValue(parContent, out ObjectType? knownType))
                    go = knownType;
                else
                {
                    go = GetTypeAsObjectType(parContent);
                }
                break;
            case ParenthesisType.Middle:
                var sp = new StringSplitEx(parContent, ':', 2);
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
                        ObjectType t = default;
                        if (KnownTypes.TryGetValue(parContent, out ObjectType? middleKnownType))
                            t = middleKnownType;
                        else
                        {
                            t = GetTypeAsObjectType(parContent);
                        }

                        if (t.Exists)
                        {
                            Type rt = (Type)t.Value;

                            List<object> contArg = new();
                            List<Type> contArgT = new();
                            if (!StrParenthesis.GetStringParenthesisType(sp[1]).HasFlag(ParenthesisType.Unknown))
                            {
                                var getArgObject = ParseArgument(sp[1], out _);
                                if (getArgObject == null || getArgObject.Value.GetType() == typeof(Unknown))
                                {
                                    ErrorData ed = new(Script.CurrentScript.CurrentLine, Script.CurrentScript.Commands[Script.CurrentScript.CurrentLine - 1].ToCommandString(), new ArgumentException("Constructor error"), "Constructer error");
                                    ExceptionOperator.SetLastErrorEx(ed);
                                    type = ParserTokenType.Unknown;
                                    return null;
                                }
                                Type cat = getArgObject.ValueType.GetValueT2();
                                dynamic toTypeObject = Convert.ChangeType(getArgObject.Value, cat);
                                if (!cat.IsArray)
                                {
                                    contArg.Add(toTypeObject);
                                    contArgT.Add(cat);
                                }
                                else
                                {
                                    var aArgs = (Array)toTypeObject;
                                    for (int i = 0; i < aArgs.Length; i++)
                                    {
                                        var argsItem = aArgs.GetValue(i) as ScriptObject;
                                        contArg.Add(argsItem.Value);
                                        contArgT.Add(argsItem.ValueType.GetValueT2());
                                    }
                                }
                            }

                            ConstructorInfo? contInfo = rt.GetConstructor(contArgT.ToArray());
                            if(contInfo == null)
                            {
                                ErrorData ed = new(Script.CurrentScript.CurrentLine, Script.CurrentScript.Commands[Script.CurrentScript.CurrentLine - 1].ToCommandString(), new ArgumentException("Unknown type constructor."), "Unknown type constructor.");
                                ExceptionOperator.SetLastErrorEx(ed);
                                type = ParserTokenType.Unknown;
                                return null;
                            }

                            try
                            {
                                object instance = Activator.CreateInstance(rt, contArg.ToArray());
                                go = new()
                                {
                                    Value = instance,
                                    ValueType = rt
                                };
                            }
                            catch
                            {
                                ErrorData ed = new(Script.CurrentScript.CurrentLine, Script.CurrentScript.Commands[Script.CurrentScript.CurrentLine - 1].ToCommandString(), new ArgumentException("Constructor error"), "Constructer error");
                                ExceptionOperator.SetLastErrorEx(ed);
                                type = ParserTokenType.Unknown;
                                return null;
                            }
                        }
                        else
                        {
                            //[customtype:<[customtype:[number:a]]>]
                            if (!StrParenthesis.GetStringParenthesisType(sp[1]).HasFlag(ParenthesisType.Unknown))
                            {
                                ScriptObject ccont = ParseArgument(sp[1], out _);
                                if(ccont == null)
                                {
                                    ErrorData ed = new(Script.CurrentScript.CurrentLine, Script.CurrentScript.Commands[Script.CurrentScript.CurrentLine - 1].ToCommandString(), new ArgumentException("Convert value failed."), "Convert value failed.");
                                    ExceptionOperator.SetLastErrorEx(ed);
                                    type = ParserTokenType.Unknown;
                                    return null;
                                }

                                List<ScriptObject> ccontArgs = default;

                                if(ccont.ValueType.GetValueT2() is Type cat && cat.IsArray)
                                {
                                    ccontArgs = new((ScriptObject[])Convert.ChangeType(ccont.Value, cat));
                                }
                                else
                                {
                                    ccontArgs = new([ccont]);
                                }

                                go = m_constantConverter[sp[0]](ccontArgs.ToArray());
                                if(go == null)
                                {
                                    ErrorData ed = new(Script.CurrentScript.CurrentLine, Script.CurrentScript.Commands[Script.CurrentScript.CurrentLine - 1].ToCommandString(), new ArgumentException("Constructor error"), "Constructer error");
                                    ExceptionOperator.SetLastErrorEx(ed);
                                    type = ParserTokenType.Unknown;
                                    return null;
                                }
                            }
                            else
                            {
                                go = new()
                                {
                                    Value = new Unknown(sp[1], sp[0]),
                                    ValueType = sp[0]
                                };
                            }
                        }
                        Type et = t.Value as Type;
                        break;
                }
                break;
            case ParenthesisType.Small:
                type = ParserTokenType.Argument;
                if (Script.CurrentScript.Vars.TryGetValue(parContent, out Variable? knownVariable))
                {
                    go = knownVariable;
                }
                else
                {
                    go = new Variable() { Name = parContent };
                    //GSScript.CurrentScript.Vars.Add(su[1..(su.Length - 2)], new GSVar() { Name = su[1..(su.Length - 2)] });
                }
                break;
            case ParenthesisType.Sharp:
                type = ParserTokenType.Argument;

                List<ScriptObject> array = new List<ScriptObject>();

                var aisp = new StringSplit(parContent, ',');
                foreach (var item in aisp.SplitUnit)
                {
                    var aiobject = ParseArgument(item.Trim(' '), out var t);
                    if(aiobject == null || t == ParserTokenType.Literal)
                    {
                        ErrorData ed = new(Script.CurrentScript.CurrentLine, Script.CurrentScript.Commands[Script.CurrentScript.CurrentLine - 1].ToCommandString(), new ArgumentException("Cannot parse the element."), "Cannot parse the element.");
                        ExceptionOperator.SetLastErrorEx(ed);
                        type = ParserTokenType.Unknown;
                        return null;
                    }
                    array.Add(aiobject);
                }

                go.Value = array.ToArray();
                go.ValueType = typeof(ScriptObject[]);
                break;
        }
        type = ParserTokenType.Argument;
        return go;
    }

    private static ObjectType GetTypeAsObjectType(string parContent)
    {
        ObjectType go;
        Type t = System.Type.GetType(parContent.Replace("$", "System.").Replace("#", "GScript.Standard"));
        if (t == null)
            go = new ObjectType(parContent);
        else
            go = new ObjectType(t);
        return go;
    }
}
