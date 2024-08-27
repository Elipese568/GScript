using GScript.Analyzer;
using GScript.Analyzer.Executing;
using GScript.Analyzer.InternalType;
using GScript.Analyzer.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScript.Standard;

public struct InvokeFrame
{
    private Dictionary<string, Variable> m_variables;
    private int m_currentLine;
    private Dictionary<string, ParserBase> m_parsers;
    private Dictionary<string, (CommandHandler, CommandArgumentOptions)> m_commandHandles;

    public static InvokeFrame CreateFrameFromScript(Script script)
    {
        InvokeFrame frame = new InvokeFrame();
        frame.m_variables = script.Vars;
        frame.m_currentLine = script.CurrentLine;
        frame.m_commandHandles = script.CommandHandlers;
        frame.m_parsers = script.CommandParsers;
        return frame;
    }

    public static void SetScriptStatusFromFrame(Script script, InvokeFrame frame)
    {
        script.Vars = frame.m_variables;
        script.CurrentLine = frame.m_currentLine;
        script.CommandHandlers = frame.m_commandHandles;
        script.CommandParsers = frame.m_parsers;
    }
}
