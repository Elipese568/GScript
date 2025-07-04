﻿namespace GScript.Standard;

using EUtility.ValueEx;
using System.CommandLine;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

internal partial class Program
{
    static RootCommand _rootCommand;

    static Entry _entry;

    static readonly Command RunCommand = new("run", "Run GScript file.");

    static readonly Argument<string> RunPathArgument = new("scriptPath", "GScript file.");

    static MemoryStream _beforeEntryFrameOutStream;

    static int Main(string[] args)
    {
//#if DEBUG
//        Process.EnterDebugMode();
//#endif
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

            incproc:
            List<string> incedLib = new(); 
            Regex incPre = MatchInc();
            string scriptContent = File.ReadAllText(path);
            var ms = incPre.Matches(scriptContent);
            foreach (var m in ms)
            {
                Match masm = ((Match)m);
                string libpath = ModuleMamager.GetModulePath(masm.Value.Split(':')[0]).Replace("%lib%", "Modules");
                string libfile = masm.Value.Split(':')[1];
                if(incedLib.Contains(libfile))
                {
                    continue;
                }
                scriptContent = scriptContent.Replace($"#inc \"{masm.Value}\"", File.ReadAllText(libpath + "\\" + libfile));
            }

            if (incPre.IsMatch(scriptContent))
                goto incproc;

            _entry = new(scriptContent);

        }, RunPathArgument);

        _rootCommand.AddCommand(RunCommand);

        _rootCommand.Invoke(args);
        return 0;
    }

    [GeneratedRegex("(?<=#inc \")[^\"]+")]
    private static partial Regex MatchInc();
}