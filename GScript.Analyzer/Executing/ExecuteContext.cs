using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScript.Analyzer.Executing;

public class ExecuteContext
{
    public int Line { get; private set; }
    public string Name { get; set; }

    public Command Command { get; set; }

    internal ExecuteContext(string name, Command command, int line)
    {
        Name = name;
        Command = command;
        Line = line;
    }
}

public class ExecuteResult
{
    public bool Complated { get; set; }
    public ExecuteContext Context { get; set; }
    public int Line { get; set; }

    public ExecuteResult(bool complated, int line, ExecuteContext context)
    {
        Complated = complated;
        Line = line;
        Context = context;
    }

    public static ExecuteResult CreateFromContext(ExecuteContext context)
    {
        return new(false, context.Line, context);
    }
}