// Date: 2023/328 22:06
// Auther: Elipese

namespace GScript.Analyzer;


public delegate bool CommandHandler(Command sender,
                                      ref int line);

public delegate void CommandGlobalHandler(Command sender, ref bool cancel, ref int line);