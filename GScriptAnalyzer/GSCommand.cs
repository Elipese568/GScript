// AT:   Elipese
// Date: 2023/2/12

using GScriptAnalyzer.Exception;
using GScriptAnalyzer.Util;
using System.Text;

namespace GScriptAnalyzer;

// Date: 2023/2/12
public class GSCommand
{
    private string m_name;
    private List<GSObject> m_args = new List<GSObject>();
    private Dictionary<GSObject, ParenthesisType> m_typeargpairs = new Dictionary<GSObject, ParenthesisType>();

    public string Name { get => m_name; set => m_name = value; }
    public List<GSObject> Args { get => m_args; set => m_args = value; }
    public Dictionary<GSObject, ParenthesisType> TypeArgPairs { get => m_typeargpairs; set => m_typeargpairs = value; }

    public GSCommand() : this("") { }

    public GSCommand(string com) : this(new StringSplit(com,' ')) { }

    GSCommand(StringSplit split)
    {
        m_name = split[0];
        foreach(string su in split[1..])
        {
            GSObject go = new GSObject();
            var part = StrParenthesis.GetStringParenthesisType(su);
            switch(part)
            {
                case ParenthesisType.Unknown:
                    throw new ArgFormatException("参数括号未知");
                case ParenthesisType.Big:
                    go = new GSType(Type.GetType("System." + su[1..(su.Length - 2)]));
                    break;
                case ParenthesisType.Middle:
                    go = new GSObject();
                    var sp = new StringSplit(su[1..(su.Length - 1)], ':');
                    switch (sp[0])
                    {
                        case "string":
                            go.Value = sp[1];
                            break;
                        case "number":
                            go.Value = Convert.ToInt64(sp[1]);
                            break;
                        case "flag":
                            go = new GSFlag(sp[1], sp[2]);
                            break;
                    }
                    break;
                case ParenthesisType.Small:
                    if(GSScript.CurrentScript.Vars.ContainsKey(su[1..(su.Length - 1)]))
                    {
                        go = GSScript.CurrentScript.Vars[su[1..(su.Length - 1)]];
                    }
                    else
                    {
                        go = new GSVar() { Name = su[1..(su.Length - 1)] };
                        //GSScript.CurrentScript.Vars.Add(su[1..(su.Length - 2)], new GSVar() { Name = su[1..(su.Length - 2)] });
                    }
                    break;
            }
            m_args.Add(go);

            m_typeargpairs.Add(go, part);
        }
        
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append("{\"CommandHead\":\"");
        sb.Append(m_name);
        sb.Append("\",\"CommandArg\":[");
        sb.Append(m_args[0]);
        foreach(GSObject gso in m_args)
        {
            sb.Append(",");
            sb.Append(gso.ToString());
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
            sb.Append(StrParenthesis.GetParenthesisTypeString(arg.Value, (arg.Value == ParenthesisType.Small) ? ((GSVar)arg.Key).Name : arg.Key.Value.ToString()));
            sb.Append(' ');
        }
        return sb.ToString().TrimEnd(' ');
    }
}
