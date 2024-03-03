using GScript.Analyzer.Exception;
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

    public StreamReader? Reader => m_reader;
    public string FileName => m_filename;
    public Head ScriptHead { get => m_scripthead; set => m_scripthead = value; }
    public List<Command> Commands { get; set; }
    public Dictionary<string, Variable> Vars { get => m_vars; set => m_vars = value; }
    public static Script? CurrentScript { get; set; }
    public List<string> Docs { get; set; }
    public Dictionary<string, (CommandHandler, CommandArgumentOptions)> CommandHandlers { get => m_cmdhandlers; set => m_cmdhandlers = value; }


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
                Vars.Add(line.Split(':')[1], new Variable() { Name = line.Split(':')[1]});
            else
                Commands.Add(new(line));
        }
        m_reader.BaseStream.Seek(0, SeekOrigin.Begin);
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
    //realline
    private int rl(int i)
    {
        return i - 1;
    }

    public bool Execute()
    {
        for(int i = 1; i <= Commands.Count;i++)
        {
            var l = rl(i);
            try
            {
                if (!m_cmdhandlers.ContainsKey(Commands[l].Name))
                {
                    ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_ILLEGALCOMMAND);
                    ExceptionOperator.SetException(new InvaildScriptSegmentException());
                    return false;
                }
                else
                {

                    bool cancel = false;
                    foreach (var kv in m_globalHandler)
                    {
                        kv(Commands[l], ref cancel, ref i);
                    }
                    if (cancel)
                        continue;

                    if (CommandArgumentOptions.VerifyArgumentFromOptions(
                        Commands[l],
                        m_cmdhandlers[Commands[l].Name].Item2) == false)
                    {
                        return false;
                    }
                    
                    if (!m_cmdhandlers[Commands[l].Name].Item1.Invoke(Commands[l], ref i))
                        return false;
                }
            }
            catch (ArgumentOutOfRangeException aoore)
            {
                ExceptionOperator.SetLastError(ExceptionOperator.GErrorCode.GSE_OUTOFLINERANGE);
                ExceptionOperator.SetException(aoore);
                return false;
            }
            catch (ArgFormatException afe)
            {
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
