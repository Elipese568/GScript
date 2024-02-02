namespace GScriptAnalyzer;

public class GSScript
{
    StreamReader m_reader;

    string m_filename;
    GSHead m_scripthead;
    Dictionary<string, GSVar> m_vars;
    Dictionary<string, (GSCommandHandler, GSCommandArgumentOptions)> m_cmdhandlers;

    public StreamReader? Reader => m_reader;
    public string FileName => m_filename;
    public GSHead ScriptHead { get => m_scripthead; set => m_scripthead = value; }
    public List<GSCommand> Commands { get; set; }
    public Dictionary<string, GSVar> Vars { get => m_vars; set => m_vars = value; }
    public static GSScript? CurrentScript { get; set; }
    public List<string> Docs { get; set; }
    public Dictionary<string, (GSCommandHandler, GSCommandArgumentOptions)> CommandHandlers { get => m_cmdhandlers; set => m_cmdhandlers = value; }

    public GSScript()
    {
        m_reader = null;
        m_filename = string.Empty;
        m_scripthead = null;
        m_vars = new Dictionary<string, GSVar>();
        m_cmdhandlers = new Dictionary<string, (GSCommandHandler, GSCommandArgumentOptions)>();
        Commands = new List<GSCommand>();
        CurrentScript = this;
    }

    ~GSScript()
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
            if (line.TrimStart(' ').StartsWith('#'))
                Docs.Add(line);
            else if (line.TrimStart(' ').StartsWith("var:"))
                Vars.Add(line.Split(':')[1], new GSVar() { Name = line.Split(':')[1]});
            else
                Commands.Add(new(line));
        }
        m_reader.BaseStream.Seek(0, SeekOrigin.Begin);
    }

    public bool RegisterCommandHandler(string cmdname, (GSCommandHandler, GSCommandArgumentOptions) handler)
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
                    GSPublic.SetLastError(GSPublic.GSErrorCode.GSE_ILLEGALCOMMAND);
                    return false;
                }
                else
                {
                    if (GSCommandArgumentOptions.VerifyArgumentFromOptions(
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
                GSPublic.SetLastError(GSPublic.GSErrorCode.GSE_OUTOFLINERANGE);
                GSPublic.SetException(aoore);
                return false;
            }
        }
        
        return true;
     }
}
