using GScript.Analyzer.Exception;
using GScript.Analyzer.Executing;
using GScript.Analyzer.InternalType;
using GScript.Analyzer.Parser;
using System.Diagnostics;

namespace GScript.Analyzer;

public class Script
{
    StreamReader m_reader;

    string m_filename;
    Head m_scripthead;
    Dictionary<string, Variable> m_vars;
    Dictionary<string, (CommandHandler, CommandArgumentOptions)> m_cmdhandlers;
    List<CommandGlobalHandler> m_globalHandler = new();
    Dictionary<string, ParserBase> m_commandParsers = new();

    public StreamReader? Reader => m_reader;
    public string FileName => m_filename;
    public Head ScriptHead { get => m_scripthead; set => m_scripthead = value; }
    public List<Command> Commands { get; set; }
    public Dictionary<string, Variable> Vars { get => m_vars; set => m_vars = value; }
    public static Script? CurrentScript { get; set; }
    public List<string> Docs { get; set; }
    public Dictionary<string, (CommandHandler, CommandArgumentOptions)> CommandHandlers { get => m_cmdhandlers; set => m_cmdhandlers = value; }
    public Dictionary<string, ParserBase> CommandParsers { get => m_commandParsers; set => m_commandParsers = value; }
    public int CurrentLine { get; set; } = 1;


    public Script()
    {
        m_reader = null;
        m_filename = string.Empty;
        m_scripthead = null;
        m_vars = new Dictionary<string, Variable>();
        m_cmdhandlers = new Dictionary<string, (CommandHandler, CommandArgumentOptions)>();
        Commands = new List<Command>();
        CurrentScript = this;
    }

    ~Script()
    {
        m_reader.Close();
    }

    public void Open(string filename)
    {
        m_reader = new StreamReader(filename);
        m_filename = filename;
        while(!m_reader.EndOfStream)
        {
            var line = m_reader.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
                continue;
            else if (line.TrimStart(' ').StartsWith('#'))
                Docs.Add(line);
            else if (line.TrimStart(' ').StartsWith("var:"))
                Vars.Add(line.Split(':')[1], new Variable() { Name = line.Split(':')[1] });
            else
            {
                ParserBase argparser = ParserBase.Empty;

                if (CommandParsers.Keys.Any(line.Contains))
                {
                    argparser = CommandParsers.First(x => line.Contains(x.Key)).Value;
                }
                else
                {
                    argparser = new DefaultCommandParser(line.Split(' ')[0]) ;
                }

                Commands.Add(new(line, argparser));
            }
        }
        m_reader.BaseStream.Seek(0, SeekOrigin.Begin);
    }

    public void OpenWithContent(string scriptContent)
    {
        const string NewLine = "\uABCD";

        string replacedContent = scriptContent.Replace("\r\n", NewLine)
                                                         .Replace("\r", NewLine)
                                                         .Replace("\n", NewLine);

        string[] contentLines = replacedContent.Split(NewLine);

        foreach(var line in contentLines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            else if (line.TrimStart(' ').StartsWith('#'))
                Docs.Add(line);
            else if (line.TrimStart(' ').StartsWith("var:"))
                Vars.Add(line.Split(':')[1], new Variable() { Name = line.Split(':')[1] });
            else
            {
                ParserBase argparser = ParserBase.Empty;

                if (CommandParsers.Keys.Any(line.Contains))
                {
                    argparser = CommandParsers.First(x => line.Contains(x.Key)).Value;
                }
                else
                {
                    argparser = new DefaultCommandParser(line.Split(' ')[0]);
                }

                Commands.Add(new(line, argparser));
            }
        }
    }

    public bool RegisterCommandHandler(string cmdname, (CommandHandler, CommandArgumentOptions) handler)
    {
        if(m_cmdhandlers.ContainsKey(cmdname))
        {
            return false;
        }
        else
        {
            m_cmdhandlers.Add(cmdname, handler);
            return true;
        }
    }

    public void RegisterGlobalCommandHandler(CommandGlobalHandler handler)
    {
        m_globalHandler.Add(handler);
    }

    public bool RegisterCommandParser(string cmdname, ParserBase parser)
    {
        if (m_commandParsers.ContainsKey(cmdname))
        {
            return false;
        }
        else
        {
            m_commandParsers.Add(cmdname, parser);
            return true;
        }
    }

    //realline
    private int rl(int i)
    {
        return i - 1;
    }

    public bool Execute()
    {
        for(int i = CurrentLine; i <= Commands.Count;i++)
        {
            var l = rl(i);
            try
            {
                if (!m_cmdhandlers.TryGetValue(Commands[l].Name, out (CommandHandler, CommandArgumentOptions) value))
                {
                    ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_ILLEGALCOMMAND);
                    ExceptionOperator.SetException(new InvaildScriptSegmentException());
                    return false;
                }
               
                bool cancel = false;
                foreach (var kv in m_globalHandler)
                {
                    kv(Commands[l], ref cancel, ref i);
                }
                if (cancel)
                    continue;

                if (CommandArgumentOptions.VerifyArgumentFromOptions(Commands[l], value.Item2) == false)
                {
                    return false;
                }

                CurrentLine = i;

                var result = value.Item1.Invoke(new ExecuteContext(Commands[l].Name, Commands[l], i));

                if (!result.Complated)
                    return false;

                i = result.Line;
            }
            catch (ArgumentOutOfRangeException aoore)
            {
                ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_OUTOFLINERANGE);
                ExceptionOperator.SetException(aoore);
                return false;
            }
        }
        
        return true;
    }

    public void SetVar(string name, object value)
    {
        Vars[name].Value = value;
    }

    public void AddVar(string name, object value = null)
    {
        Vars.Add(name, new Variable() { Name = name, Value = value });
    }

    public void RemoveVar(string name)
    {
        Vars.Remove(name);
    }
}
