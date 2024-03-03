namespace GScript.Standard;

using EUtility.ValueEx;
using System.CommandLine;
using System.Diagnostics;

internal class Program
{
    static RootCommand _rootCommand;

    static Entry _entry;

    static readonly Command RunCommand = new("run", "Run GScript file.");

    static readonly Argument<string> RunPathArgument = new("scriptPath", "GScript file.");

    static MemoryStream _beforeEntryFrameOutStream;

    static int Main(string[] args)
    {
#if DEBUG
        Process.EnterDebugMode();
#endif
        _rootCommand = new RootCommand();
        _beforeEntryFrameOutStream = new MemoryStream();

        RunCommand.AddAlias("r");

        RunCommand.AddArgument(RunPathArgument);

        RunCommand.SetHandler((path) =>
        {
            if (!File.Exists(path))
            {
                Console.WriteLine("Error: The script file isn't exists.\n" +
                                        "(Maybe is an internet file or a System Memory File?)");
                return;
            }

            _entry = new(path);
            
        }, RunPathArgument);

        _rootCommand.AddCommand(RunCommand);

        _rootCommand.Invoke(args);
        return 0;
    }
}