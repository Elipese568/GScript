// Date: 2023/328 22:06
// Auther: Elipese

namespace GScript.Analyzer.Executing;


public delegate ExecuteResult CommandHandler(ExecuteContext context);

public delegate void CommandGlobalHandler(Command sender, ref bool cancel, ref int line);