using EUtility.ValueEx;
using GScript.Analyzer.InternalType;
using GScript.Analyzer.Util;
using System.CommandLine;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;

using TrueColorConsole;

namespace GScript.Editor;


internal partial class Program
{
    static string _fn = string.Empty;

    static string FileName
    {
        get => _fn;
        set
        {
            Console.Title = "Generic Script Editor - " + Path.GetFileName(value);
            _fn = value;
        }
    }

    static Func<T> DefaultValue<T>(T value) => () => value;

    static void Main(string[] args)
    {
        Setting.Init(); // NO REMOVE!!!

        RootCommand _root = new();

        var arg = new Argument<string>("ConfigFile", () =>
        {
            if (File.Exists("Default.json"))
                return "Default.json";
            return null;
        }, "Editor analyzer and style config file.");

        var SetDefault = new Command("DefaultConfig", "Set default analyzer and style config file. \n Can input 'None' to reset.");
        SetDefault.AddArgument(arg);

        SetDefault.SetHandler((p) =>
        {
            if (!File.Exists(p))
            {
                Console.WriteLine($"Cannot found file {p}.");
                return;
            }

            if (!File.Exists("Default.json"))
            {
                File.Copy(p, "Default.json");
            }
            else
            {
                Console.WriteLine("Default config file exists.\n Do you want to replace this?(Y/N)");
                if (Console.Read() != 'Y')
                    Environment.Exit(0);
                File.Copy(p, "Default.json", true);
            }
        }, arg);

        var Open = new Command("Open", "Open file to edit.");
        var openArg = new Argument<string>("OpenFile", DefaultValue(string.Empty), "Input a path of file to open.");
        var quietOption = new Option<bool>("Quiet", DefaultValue(true), "When want you to which options, always choose 'Yes'");

        Open.AddArgument(arg);
        Open.AddArgument(openArg);
        _root.AddGlobalOption(quietOption);

        Open.SetHandler((q, configPath, openFile) =>
        {
            string resolvedConfig = ResolveConfigPath(configPath, q);
            if (resolvedConfig == null)
                Environment.Exit(-1);

            // 这里直接加载配置对象并传递
            var config = JsonSerializer.Deserialize<StyleTable>(File.ReadAllText(resolvedConfig));
            var session = new Editor(config);
            session.Run();
        }, quietOption, arg, openArg);

        _root.AddArgument(arg);
        _root.AddCommand(SetDefault);

        _root.SetHandler((q, configPath) =>
        {
            string resolvedConfig = ResolveConfigPath(configPath, q);
            if (resolvedConfig == null)
                Environment.Exit(-1);

            var config = JsonSerializer.Deserialize<StyleTable>(File.ReadAllText(resolvedConfig));
            var session = new Editor(config);
            session.Run();
        }, quietOption, arg);

        _root.Invoke(args);
    }
    static string ResolveConfigPath(string configPath, bool quiet)
    {
        if (!File.Exists(configPath))
        {
            string defaultConfig = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Default.json");
            if (File.Exists(defaultConfig))
            {
                if (!quiet)
                {
                    Console.WriteLine($"Cannot found file {configPath}. Do you want to use Default config? (Y/N)");
                    if (Console.Read() != 'Y')
                        return null;
                }
                return defaultConfig;
            }
            else
            {
                Console.WriteLine($"Cannot found file {configPath}");
                return null;
            }
        }
        return configPath;
    }
}

internal record struct ContentFrame(List<List<KeyUnit>> contents, List<string> raw_contents, int row, int col, int page)
{
    public static implicit operator (List<List<KeyUnit>> contents, List<string> raw_contents, int row, int col, int page)(ContentFrame value)
    {
        return (value.contents, value.raw_contents, value.row, value.col, value.page);
    }

    public static implicit operator ContentFrame((List<List<KeyUnit>> contents, List<string> raw_contents, int row, int col, int page) value)
    {
        ContentFrame cf = new();
        (cf.row, cf.page, cf.col) = (value.row, value.page, value.col);
        cf.contents = value.contents.ToList();
        cf.raw_contents = value.raw_contents.ToList();
        return cf;
    }
}