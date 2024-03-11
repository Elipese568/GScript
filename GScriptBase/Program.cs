namespace GScript.Standard;

using EUtility.ValueEx;
using System.CommandLine;
using System.Diagnostics;
using System.Text;

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

            //string[] content = File.ReadAllLines(path);

            //StringBuilder sb = new();

            //foreach(var line in content)
            //{
            //    if(line.StartsWith("#inc"))
            //    {
            //        sb.AppendJoin(Environment.NewLine, File.ReadAllLines(line[4..].Replace(".\\", "std\\") + ".gs"));
            //        sb.AppendLine();
            //    }
            //    else
            //    {
            //        sb.AppendLine(line);
            //    }
            //}

            //File.WriteAllText(Path.GetTempFileName(), sb.ToString());

            _entry = new(path);
            
        }, RunPathArgument);

        _rootCommand.AddCommand(RunCommand);

        _rootCommand.Invoke(args);
        return 0;
    }
}