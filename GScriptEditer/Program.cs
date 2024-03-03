using GScript.Analyzer;
using GScript.Analyzer.Exception;
using GScript.Analyzer.Util;
using Microsoft.Win32;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel.Design;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Net.Mime;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using TrueColorConsole;
using static System.Net.Mime.MediaTypeNames;

namespace GScriptEditer;


internal class Program
{
    static List<List<KeyUnit>> Units = new();
    static List<string> raw = new();

    static Stack<ContentFrame> History = new();
    static Stack<ContentFrame> BackTemp = new();

    static StyleTable ConfigStyle = new();

    static Regex _digit = new(@"\d+");
    static Regex _string = new(@"(""{0,1}[^\s]+""{0,1})|(""(.\s)+"")");
    static Regex _char = new(@"('{0,1}[^\s]+'{0,1})|('\s')");
    static Regex _float = new(@"\d+.\d+");
    static Regex _boolean = new(@"true|false");
    static Regex _any = new Regex(@".*");
    static Dictionary<KeyType, Regex> _valueVaildRegularExpressions = new()
    {
        [KeyType.Digit] = _digit,
        [KeyType.Float] = _float,
        [KeyType.String] = _string,
        [KeyType.Boolean] = _boolean,
        [KeyType.Char] = _char,
        [KeyType.Tag] = _string,
        [KeyType.Text] = _any
    };

    static ColorSet _fore = new()
    {
        R = 255,
        G = 255,
        B = 255,
    };

    static ColorSet _back = new()
    {
        R = 0,
        G = 0,
        B = 0
    };

    static ColorSet _none = new()
    {
        R = -1,
        G = -1,
        B = -1
    };

    static void Print(int page)
    {
        Console.Clear();
        Console.Write("\x1b[3J");
        
        int start = page * Console.WindowHeight;
        int end = start + Console.WindowHeight;
        int line = 1 + start;
        if (start > Units.Count)
            return;

        var list = (Units ?? new());
        foreach (var l in list.ToArray()[start..(Math.Min(end, list.Count))])
        {
            Console.Write("      ");
            Console.CursorLeft = 0;
            Console.Write(line);
            Console.CursorLeft = 6;
            foreach(var u in l)
            {
                if (ConfigStyle.ColorStyle[u.Type].ForegroundColor.Equals(_none))
                {
                    TrueColorConsole.VTConsole.SetColorForeground((Color)_fore);
                }
                else
                {
                    TrueColorConsole.VTConsole.SetColorForeground((Color)ConfigStyle.ColorStyle[u.Type].ForegroundColor);
                }

                if (ConfigStyle.ColorStyle[u.Type].BackgroundColor.Equals(_none))
                {
                    TrueColorConsole.VTConsole.SetColorBackground((Color)_back);
                }
                else
                {
                    TrueColorConsole.VTConsole.SetColorBackground((Color)ConfigStyle.ColorStyle[u.Type].BackgroundColor);
                }
                TrueColorConsole.VTConsole.Write(u.RawString);
            }
            if(line != end)
                TrueColorConsole.VTConsole.WriteLine();
            line++;
        }
    }

    static bool IsOnlyType(string str, Regex r)
    {
        return r.Match(str).Value.Length == str.Length;
    }

    enum FoucsMode
    {
        NoSelected,
        Selected
    }

    struct DrawData
    {
        public FoucsMode Mode { get; set; }
        public Rectangle Size { get; set; }
    }

    interface IMenuItem
    {
        public void Draw(DrawData data);
        public bool CanSelected();
        public void Selected();
    }

    class TextMenuItem : IMenuItem
    {
        public TextMenuItem(string text, Action selected)
        {
            Text = text;
            SelectedEvent += selected;
        }
        
        public string Text { get; set; }

        public event Action SelectedEvent;

        public void Draw(DrawData data)
        {
            int width = data.Size.Width;

            void DrawInternal(Color fore, Color back, int leftpadding)
            {
                VTConsole.SetColorForeground(fore);
                VTConsole.SetColorBackground(back);
                for (int i = 0; i < width; i++)
                {
                    if(i < leftpadding || i - leftpadding >= Text.Length)
                    {
                        Console.Write(' ');
                        continue;
                    }

                    Console.Write(Text[i - leftpadding]);
                }
            }

            
            switch (data.Mode)
            {
                case FoucsMode.NoSelected: DrawInternal(_back, _fore, 1); break;
                case FoucsMode.Selected: DrawInternal(_fore, _back, 1); break;
            }

            return;
        }

        public bool CanSelected() => true;

        public void Selected()
        {
            SelectedEvent();
        }
    }

    class SeparationItem : IMenuItem
    {
        public void Draw(DrawData data)
        {
            VTConsole.SetColorForeground(_fore);
            VTConsole.SetColorBackground(_back);
            int padding = 1;
            int width = data.Size.Width;
            for (int i = 0; i < width; i++)
            {
                if (i < 1 || i > width - padding - 1)
                {
                    Console.Write(' ');
                    continue;
                }

                Console.Write('-');
            }
        }
        public bool CanSelected() => false;
        public void Selected() { throw new NotImplementedException(); }
    }

    [DllImport("User32.dll")]
    public extern static int MessageBoxA(IntPtr ptr, string message, string caption, uint flag);

    static void Menu(int page)
    {
        VTConsole.CursorAnsiSave();
        Console.CursorVisible = false;
        Console.SetCursorPosition(0, 0);
        List<KeyValuePair<string, List<IMenuItem>>> MenuItems = new()
        {
            KeyValuePair.Create("File", new List<IMenuItem>
            {
                new TextMenuItem("Open", () =>
                {
                    var item = ~FileList.FileDialog(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                    if(item.IsVoid())
                        return;

                    FileInfo fi = item as FileInfo;
                    if(string.Concat(raw).Length != 0)
                    {
                        long ync = 0x3L;
                        long infomationicon = 0x00000040L;

                        int result = MessageBoxA(new(0), $"This editer has content, do you want to save this to {(string.IsNullOrEmpty(FileName)? "untitled.gs" : FileName)}?", "Caption", (uint)(ync | infomationicon));

                        int yes = 6;
                        int no = 7;
                        int cancel = 2;

                        if(result == cancel)
                            return;

                        if(result == yes)
                        {
                            List<string> content = GetContentArea();
                            if(string.IsNullOrEmpty(FileName))
                            {
                                var file = ~FileList.FileDialog(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                                if(file.IsVoid())
                                    return;

                                File.WriteAllLines((file as FileInfo).FullName, content);
                            }
                            else
                            {
                                if (!File.Exists(FileName))
                                    File.Create(FileName).Close();
                                File.WriteAllLines(FileName, content);
                            }
                        }

                    }

                    var t = File.ReadAllLines(fi.FullName);
                    Units = new();
                    raw = new();
                    for(int p = 0; p < 4000; p++)
                    {
                        raw.Add("");
                    }

                    Dictionary<string, KeyUnit> keys = new();
                    var all = ConfigStyle.GetList();
                    all.ForEach(x => keys.Add(x.RawString, x));
                    int i = 0;
                    foreach(var line in t)
                    {
                        var lineunits = GetUnits(keys, line);
                        raw[i] = line;
                        Units.Add(lineunits);
                        i++;
                    }
                    FileName = fi.FullName;
                    col = 6;
                    row = 0;
                }),
                new SeparationItem(),
                new TextMenuItem("Save", () =>
                {
                    var content = GetContentArea();
                    if(string.IsNullOrEmpty(FileName))
                    {
                        var file = ~FileList.FileDialog(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                        if(file.IsVoid())
                            return;

                        File.WriteAllLines((file as FileInfo).FullName, content);
                    }
                    else
                    {
                        if (!File.Exists(FileName))
                            File.Create(FileName).Close();
                        File.WriteAllLines(FileName, content);
                    }
                }),
                new TextMenuItem("Save as", () =>
                {
                    var content = GetContentArea();

                    var file = ~FileList.FileDialog(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                    if(file.IsVoid())
                        return;

                    FileInfo fi = file as FileInfo;
                    File.WriteAllLines(fi.FullName, content);
                    FileName = fi.FullName;
                })
            }),
            KeyValuePair.Create("Run", new List<IMenuItem>()
            {
                new TextMenuItem("Run script", () =>
                {
                    var content = GetContentArea();
                    string path = ConfigStyle.InterpreterPath;
                    if(string.IsNullOrEmpty(FileName))
                    {
                        var file = ~FileList.FileDialog(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                        if(file.IsVoid())
                            return;

                        File.WriteAllLines((file as FileInfo).FullName, content);
                        FileName = (file as FileInfo).FullName;
                    }
                    else
                    {
                        if (!File.Exists(FileName))
                            File.Create(FileName).Close();
                        File.WriteAllLines(FileName, content);
                    }

                    Process.Start(path, new[]{"r", FileName });
                })
            })
        };

        void SetAccent(bool rev = false)
        {
            if (rev)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.White;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Black;
            }
        }

        int selectMain = 0;
        

        Dictionary<string, int> leftMap = new();

        while (true)
        {
            Print(page);
            Console.SetCursorPosition(0, 0);
            for (int i = 0; i < Console.WindowWidth; i++)
            {
                Console.Write(' ');
            }
            Console.SetCursorPosition(0, 0);
            for (int i = 0; i < MenuItems.Count; i++)
            {
                SetAccent(i == selectMain);
                var item = MenuItems[i];
                if (!leftMap.ContainsKey(item.Key))
                    leftMap.Add(item.Key, Console.CursorLeft);
                for (int j = 0; j < 8; j++)
                {
                    if (j < item.Key.Length)
                    {
                        Console.Write(item.Key[j]);
                    }
                    else
                    {
                        Console.Write(' ');
                    }
                }
                Console.ResetColor();
                Console.Write("  ");
            }
            Console.ResetColor();
            var key = Console.ReadKey(false);

            if(key.Key == ConsoleKey.LeftArrow)
            {
                selectMain--;
                if(selectMain < 0)
                {
                    selectMain = 0;
                }
            }
            else if(key.Key == ConsoleKey.RightArrow)
            {
                selectMain++;
                if(selectMain >= MenuItems.Count)
                {
                    selectMain  = MenuItems.Count - 1;
                }
            }
            else if(key.Key == ConsoleKey.Enter)
            {
                PrintSub(MenuItems[selectMain].Value, leftMap[MenuItems[selectMain].Key], 16);
                Console.CursorVisible = true;
                VTConsole.CursorAnsiRestore();
                return;
            }
        }
        
        void PrintSub(List<IMenuItem> items, int start, int width)
        {
            int selectSub = 0;
            bool up = false;
            while (true)
            {
                Console.ResetColor();
                Console.SetCursorPosition(start, 1);

                for (int i = 0; i < items.Count; i++)
                {
                    Console.CursorLeft = start;
                    Console.Write(new string(' ', width));
                    Console.CursorTop++;
                }
                Console.ResetColor();
                Console.SetCursorPosition(start, 1);

                for (int j = 0; j < items.Count; j++)
                {
                    if (selectSub != j)
                    {
                        items[j].Draw(new()
                        {
                            Mode = FoucsMode.NoSelected,
                            Size = new Rectangle(0, 0, width, 1)
                        });
                    }
                    else if (selectSub == j)
                    {
                        if (!items[j].CanSelected())
                        {
                            selectSub += up ? -1 : 1;
                            if (selectSub >= items.Count || selectSub < 0)
                            {
                                selectSub = 0;
                            }
                        }
                        items[j].Draw(new()
                        {
                            Mode = FoucsMode.Selected,
                            Size = new Rectangle(0, 0, width, 1)
                        });
                    }
                    Console.CursorLeft = start;
                    Console.CursorTop++;
                }
                Console.ResetColor();
                var key = Console.ReadKey();
                if (key.Key == ConsoleKey.UpArrow)
                {
                    selectSub--;
                    if (selectSub == -1)
                    {
                        selectSub = 0;
                    }
                    up = true;
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    selectSub++;
                    if (selectSub >= items.Count)
                    {
                        selectSub = 0;
                    }
                    up = false;
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    items[selectSub].Selected();
                    return;
                }
                else if (key.Key == ConsoleKey.Escape)
                    return;
            }
        }
    }

    private static List<string> GetContentArea()
    {
        List<string> line = new();
        int whitespacecount = 0;

        foreach (var l in raw)
        {
            if (whitespacecount > 8)
            {
                line.RemoveRange(line.Count - 1 - 8, 8);
                break;
            }
            if (string.IsNullOrWhiteSpace(l))
            {
                whitespacecount++;
            }
            line.Add(l);
        }

        return line;
    }
    static string _fn = "";

    static string FileName
    {
        get => _fn;
        set
        {
            Console.Title = "Generic Script Editor - " + Path.GetFileName(value);
            _fn = value;
        }
    }

    static int col = 6;
    static int row = 0;
    static int previewpage = 0;
    static int editpage = 0;

    static int EditRow => row + editpage * Console.WindowHeight;

    static void Main(string[] args)
    {
        Setting.Init(); // NO REMOVE!!!

        Units.Add(new());
        for (int i = 0; i < 4000; i++)
        {
            raw.Add("");
        }

        RootCommand _root = new();

        var arg = new Argument<string>("ConfigFile", () =>
        {
            if (File.Exists("Default.json"))
                return "Default.json";

            return null;
        }, "Editor analyzer and style config file.");

        var SetDefault = new System.CommandLine.Command("DefaultConfig", "Set default analyzer and style config file. \n Can input 'None' to reset.");

        SetDefault.AddArgument(arg);

        SetDefault.SetHandler((p) =>
        {
            if (!File.Exists(p))
                Console.WriteLine($"Cannot fount file {p}.");

            if (!File.Exists("Default.json"))
                File.Copy(p, "Default.json");
            else
            {
                Console.WriteLine("Default config file exists.\n Do you want to replace this?(Y/N)");
                if (Console.Read() != 'Y')
                    Environment.Exit(0);
                File.Copy(p, "Default.json");
            }
        }, arg);

        var Open = new System.CommandLine.Command("Open", "Open file to edit.");
        var openArg = new Argument<string>("OpenFile", () => "", "Input a path of file to open.");
        var quietOption = new Option<bool>("Quiet", "When want you to which options, always choose 'Yes'");

        Open.AddArgument(arg);
        Open.AddArgument(openArg);
        _root.AddGlobalOption(quietOption);

        Open.SetHandler((q, c, p) =>
        {
            if (!File.Exists(c))
            {
                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Default.json")))
                {
                    if (q)
                        goto M;
                    Console.WriteLine($"Cannot found file {c}. Do you want to use Default config? (Y/N)");
                    if (Console.Read() != 'Y')
                        Environment.Exit(-1);
                }
                else
                {
                    Console.WriteLine($"Cannot found file {c}");
                    Environment.Exit(-1);
                }
M:
                EditorMain(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Default.json"), p);
            }
            else
            {
                EditorMain(c, p);
            }
            
        }, quietOption, arg, openArg);



        _root.AddArgument(arg);
        _root.AddCommand(SetDefault);

        _root.SetHandler((q, p) =>
        {
            if (!File.Exists(p))
            {
                if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Default.json")))
                {
                    if(q) goto EM;
                    Console.WriteLine($"Cannot found file {p}. Do you want to use Default config? (Y/N)");
                    if (Console.Read() != 'Y')
                        Environment.Exit(-1);
                }
                else
                {
                    Console.WriteLine($"Cannot found file {p}");
                    Environment.Exit(-1);
                }
EM:
                EditorMain(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Default.json"));
            }
            else
            {
                EditorMain(p);
            }
            
        }, quietOption, arg);

        _root.Invoke(args);
    }

    static void EditorMain(string configpath, string? filepath = null)
    {
        ConfigStyle = JsonSerializer.Deserialize<StyleTable>(File.ReadAllText(configpath), new JsonSerializerOptions
        {
            WriteIndented = true
        });

        Dictionary<string, KeyUnit> keys = new();
        var all = ConfigStyle.GetList();
        all.ForEach(x => keys.Add(x.RawString, x));

        string currentstring = "";

        int tabc = 0;

        bool isPreviewMode = false;

        while (true)
        {
        whileStart:
            Print(isPreviewMode ? previewpage : editpage);
        whileStartNonPrint:
            VTConsole.CursorPosition(row + 1, col + 1);
            var key = Console.ReadKey();

            if (key.Key == ConsoleKey.F1)
            {
                Menu(isPreviewMode ? previewpage : editpage);
                VTConsole.SetScrollingRegion(1, 1);
                continue;
            }
            if (key.Key == ConsoleKey.S && key.Modifiers == ConsoleModifiers.Control)
            {
                var content = GetContentArea();
                if (string.IsNullOrEmpty(FileName))
                {
                    var file = ~FileList.FileDialog(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                    if (file.IsVoid())
                        return;

                    File.WriteAllLines((file as FileInfo).FullName, content);
                }
                else
                {
                    if (!File.Exists(FileName))
                        File.Create(FileName).Close();
                    File.WriteAllLines(FileName, content);
                }
                continue;
            }
            else if (key.Key == ConsoleKey.S && key.Modifiers == (ConsoleModifiers.Control | ConsoleModifiers.Shift))
            {
                var content = GetContentArea();
                var file = ~FileList.FileDialog(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                if (file.IsVoid())
                    return;

                File.WriteAllLines((file as FileInfo).FullName, content);
            }
            else if(key.Key == ConsoleKey.Z && key.Modifiers == ConsoleModifiers.Control && History.Count != 0)
            {
                (var u, List<string> ru, int r, int c, int p) = History.Pop();
                BackTemp.Push((u,ru,r,c,p));
                Units = u;
                raw = ru;
                row = r;
                col = c;
                editpage = p;
                goto whileStart;
            }
            else if(key.Key == ConsoleKey.Y && key.Modifiers == ConsoleModifiers.Control && BackTemp.Count != 0)
            {
                (var u, List<string> ru, int r, int c, int p) = BackTemp.Pop();
                History.Push((u, ru, r, c, p));
                Units = u;
                raw = ru;
                row = r;
                col = c;
                editpage = p;
                goto whileStart;
            }
            else if(key.KeyChar is ' ' or '\n' || key.KeyChar > '0')
            {
                // Other keys
                if (BackTemp.Count != 0)
                    BackTemp.Clear();
            }
            #region EditKeys
            if ((key.Key is
                ConsoleKey.DownArrow or
                ConsoleKey.UpArrow or
                ConsoleKey.LeftArrow or
                ConsoleKey.RightArrow
                &&
                Units.Count == 0))
                goto whileStart;
            else if (key.Key == ConsoleKey.UpArrow && row == 0 && editpage == 0)
                goto whileStart;
            switch (key.Key)
            {
                case ConsoleKey.DownArrow:
                    isPreviewMode = false;
                    row++;

                    if (row >= Console.WindowHeight && editpage > 0)
                    {
                        editpage--;
                        row = 0;
                    }
                    else if (row < Console.WindowHeight)
                    {
                        goto whileStartNonPrint;
                    }

                    if (raw[EditRow].Length != 0 && raw[EditRow].Length + 6 < col)
                    {
                        VTConsole.CursorAbsoluteVertical(raw[EditRow].Length - 2);
                        col = raw[EditRow].Length + 6;
                    }
                    else if (raw[EditRow].Length == 0)
                    {
                        col = 6;
                    }
                    goto whileStartNonPrint;
                case ConsoleKey.UpArrow:
                    isPreviewMode = false;
                    row--;

                    if (row < 0 && editpage > 0)
                    {
                        editpage--;
                        row = Console.WindowHeight - 1;
                        if (raw[EditRow].Length == 0)
                        {
                            col = 6;
                        }
                        goto whileStart;
                    }
                    else if (row < 0)
                    {
                        row++;
                        goto whileStartNonPrint;
                    }

                    if (raw[EditRow].Length != 0 && raw[EditRow].Length + 6 < col)
                    {
                        VTConsole.CursorAbsoluteVertical(raw[EditRow].Length + 6);
                        col = raw[EditRow].Length + 6;
                    }
                    else if (raw[EditRow].Length == 0)
                    {
                        col = 6;
                    }
                    goto whileStartNonPrint;
                case ConsoleKey.LeftArrow:
                    isPreviewMode = false;
                    col--;
                    if (col < 6)
                    {
                        col = 6;
                    }
                    goto whileStartNonPrint;
                case ConsoleKey.RightArrow:
                    isPreviewMode = false;
                    col++;
                    if (col >= raw[EditRow].Length + 6)
                    {
                        col = raw[EditRow].Length + 6;
                    }
                    goto whileStartNonPrint;
                case ConsoleKey.Enter:
                    isPreviewMode = false;
                    if (row == Console.WindowHeight - 1)
                    {
                        Units.Add(new());
                        row = 0;
                        editpage++;
                        col = tabc * 4 + 6;
                        goto whileStart;
                    }
                    if (col < currentstring.Length + 6)
                    {
                        string sub = currentstring.Substring(col - 6);
                        string front = currentstring[..(col - 6)];
                        var l1 = GetUnits(keys, sub);
                        var l2 = GetUnits(keys, front);
                        Units[EditRow] = l2;
                        raw[EditRow] = front;
                        raw[row + 1] = sub;
                        if (Units.Count - 1 < row + 1)
                        {
                            Units.Add(Enumerable.Repeat(new KeyUnit() { RawString = "    ", Type = KeyType.Text }, tabc).ToList());
                            raw.Add(string.Join("", Enumerable.Repeat("    ", tabc)));
                            Units.Add(l1);
                        }
                        else
                        {
                            raw.Insert(row + 1, string.Join("", Enumerable.Repeat("    ", tabc)));
                            Units.Insert(row + 1, Enumerable.Repeat(new KeyUnit() { RawString = "    ", Type = KeyType.Text }, tabc).ToList());

                            Units[row + 1].AddRange(l1);
                        }
                        row++;
                        col = tabc * 4 + 6;
                        currentstring = raw[EditRow];
                        goto whileStart;
                    }
                    else if (col == 6)
                    {
                        if (Units.Count <= row + Console.WindowHeight * editpage + 1)
                        {
                            Units.Add(Enumerable.Repeat(new KeyUnit() { RawString = "    ", Type = KeyType.Text }, tabc).ToList());
                            raw.Add(string.Join("", Enumerable.Repeat("    ", tabc)));
                        }
                        row++;
                        col = tabc * 4 + 6;
                        currentstring = raw[EditRow];
                        goto whileStart;
                    }

                    if (Units.Count < row + 1)
                    {
                        Units.Add(new());
                        raw.Insert(row + 1, string.Join("", Enumerable.Repeat("    ", tabc)));
                        Units.Insert(row + 1, Enumerable.Repeat(new KeyUnit() { RawString = "    ", Type = KeyType.Text }, tabc).ToList());
                        row++;
                        col = tabc * 4 + 6;
                        currentstring = raw[EditRow];
                        goto whileStart;
                    }
                    else if (Units.Count >= row + 1)
                    {
                        raw.Insert(row + 1, string.Join("", Enumerable.Repeat("    ", tabc)));
                        Units.Insert(row + 1, Enumerable.Repeat(new KeyUnit() { RawString = "    ", Type = KeyType.Text }, tabc).ToList());
                        row++;
                        col = tabc * 4 + 6;
                        currentstring = raw[EditRow];
                    }

                    col = tabc * 4 + 6;
                    goto whileStart;
                case ConsoleKey.End:
                    isPreviewMode = false;
                    col = raw[EditRow].Length + 6;
                    goto whileStart;
                case ConsoleKey.Home:
                    isPreviewMode = false;
                    col = 6;
                    goto whileStart;
                case ConsoleKey.Tab:
                    isPreviewMode = false;
                    if (Units.Count <= row)
                    {
                        Units.Add(new());
                        raw.Insert(EditRow, "");
                    }
                    if (Units[EditRow].Count == 0)
                    {
                        Units[EditRow].Add(MakeUnit("    ", KeyType.Text));

                        raw[EditRow] += "    ";
                    }
                    else if ((col - 6) % 4 == 0)
                    {
                        Units[EditRow].Add(MakeUnit("    ", KeyType.Text));
                        raw[EditRow] = raw[EditRow].Insert(col - 6, "    ");
                    }
                    else if ((col - 6) % 4 != 0)
                    {
                        goto whileStart;
                    }
                    else if (IsOnlyType(raw[EditRow][..(col - 6)], new(@"\s+")))
                    {
                        Units[EditRow].Add(MakeUnit("    ", KeyType.Text));
                        raw[EditRow] = raw[EditRow].Insert(col - 6, "    ");
                    }

                    col += 4;
                    tabc++;
                    goto whileStart;
                case ConsoleKey.PageUp:
                    if (isPreviewMode && previewpage > 0)
                        previewpage--;
                    else
                    {
                        previewpage = editpage - 1;
                        isPreviewMode = true;
                    }
                    goto whileStart;
                case ConsoleKey.PageDown:
                    if (isPreviewMode)
                        previewpage++;
                    else
                    {
                        previewpage = editpage + 1;
                        isPreviewMode = true;
                    }
                    goto whileStart;
            }

            currentstring = raw[EditRow];

            if (key.Key is ConsoleKey.Backspace or ConsoleKey.Delete)
            {
                isPreviewMode = false;
                if (col == 6 && row == 0 && editpage > 0)
                {
                    if (raw[EditRow].Length == 0)
                    {
                        Units.RemoveAt(EditRow);
                        if (raw[row + editpage * Console.WindowHeight - 1].Length > 0)
                        {
                            col = raw[row - 1].Length + 6;
                        }
                    }
                    row = Console.WindowHeight - 1;
                    editpage--;
                    goto whileStart;
                }

                if (col != 6 && (col - 6) % 4 == 0 && Units[EditRow].Count >= (col == 6 ? 0 : (col - 6) / 4) && Units[EditRow][(col - 6) / 4 - 1].RawString == "    ")
                {
                    if (tabc > 0)
                        tabc--;
                    Units[EditRow].RemoveAt((col - 6) / 4 - 1);
                    raw[EditRow] = raw[EditRow].Remove((col - 6) / 4 - 1, 4);
                    col -= 4;
                    goto whileStart;
                }
                if (row == 0 && currentstring.Length == 0)
                {
                    if (Units.Count == 0)
                        goto whileStart;
                    Units.Remove(Units[EditRow]); goto whileStart;
                }
                else if (col == 6)
                {
                    if (raw[EditRow].Length == 0 && row > 0)
                    {
                        Units.RemoveAt(EditRow);
                        row--;
                        if (raw[EditRow].Length > 0)
                        {
                            col = raw[EditRow].Length + 6;
                        }
                        goto whileStart;
                    }
                    string bf = raw[row - 1];
                    bf += raw[EditRow];
                    raw[EditRow] = "";
                    if (row < Units.Count)
                        Units.RemoveAt(EditRow);
                    raw[row - 1] = bf;
                    currentstring = bf;
                    row--;
                    col = bf.Length + 6;
                    var l = GetUnits(keys, currentstring);
                    Units[EditRow] = l;
                    goto whileStart;
                }
                if (currentstring.Length == 0)
                {
                    if (Units.Count - 1 >= row && Units[EditRow] != null)
                    {
                        Units[EditRow] = null;
                        Units.Remove(Units[EditRow]);
                    }
                    col = raw[row - 1].Length + 6;
                    row--;
                    goto whileStart;
                }
                else if (col < currentstring.Length + 6)
                {
                    currentstring = currentstring.Remove(col - 7, 1);
                    col--;
                }
                else
                {
                    currentstring = currentstring[..^1];
                    col--;
                }
            }
            else
            {
                isPreviewMode = false;
                if (col - 6 == currentstring.Length)
                    currentstring += key.KeyChar;
                else
                    currentstring = currentstring.Insert(col - 6, key.KeyChar.ToString());
                col++;
            }
            #endregion
            List<KeyUnit> ku = GetUnits(keys, currentstring);

            if (Units.Count - 1 < row)
                Units.Add(new());
            Units[EditRow] = ku;
            raw[EditRow] = currentstring;

            if (!ku.Exists((x) => x.Type == KeyType.Text))
                History.Push((Units, raw, row, col, editpage));
        }
    }

    static KeyUnit MakeUnit(string Rawstring, KeyType KeyType)
            => new() { Type = KeyType, RawString = Rawstring };

    static List<KeyUnit> GetUnits(Dictionary<string, KeyUnit> keys, string currentstring)
    {
        List<KeyUnit> ku = new();
        int i = 0;

        while (currentstring.Length > 0 && i < currentstring.Length && currentstring[i] == ' ')
        {
            if (i != 0 && (i + 1) % 4 == 0)
            {
                ku.Add(MakeUnit("    ", KeyType.Text));
                currentstring = currentstring.Remove(0, 4);
                i = 0;
                continue;
            }
            i++;
        }
        if (i % 4 != 0)
        {
            ku.Add(MakeUnit(new(' ', i % 4), KeyType.Text));
            currentstring.Remove(0, i % 4);
        }

        StringSplit split = new(currentstring, ' ');

        if (split.SplitUnit.Length == 0)
            return new();
        if (keys.ContainsKey(split[0]))
            ku.Add(keys[split[0]]);
        else
            ku.Add(MakeUnit(split[0], KeyType.Text));

        ku.Add(MakeUnit(" ", KeyType.Split));

        foreach (string su in split[1..])
        {
            ScriptObject go = new ScriptObject();
            var part = StrParenthesis.GetStringParenthesisType(su);
            switch (part.HasFlag(ParenthesisType.Half) ? part ^ ParenthesisType.Half : part)
            {
                case ParenthesisType.Unknown:
                    ku.Add(MakeUnit(su, KeyType.Text));
                    break;
                case ParenthesisType.Big:
                    ku.Add(new() { RawString = su, Type = KeyType.Type });
                    break;
                case ParenthesisType.Middle:
                    go = new ScriptObject();
                    ku.Add(MakeUnit("[", KeyType.Parenthesis));
                    if (su.Length == 1)
                        break;
                    var su2 = su[1..(part.HasFlag(ParenthesisType.Half) ? ^0 : ^1)];
                    if (!part.HasFlag(ParenthesisType.Half) && su2.Length == 0)
                        goto MiddleEnd;
                    if (!su2.Contains(":"))
                    {
                        ku.Add(MakeUnit(su2, KeyType.Text));
                        goto MiddleEnd;
                    }
                    var sp = new StringSplit(su2, ':');
                    if (keys.ContainsKey(sp[0]) && keys[sp[0]].Type == KeyType.KnownType)
                        ku.Add(keys[sp[0]]);
                    else
                        ku.Add(MakeUnit(sp[0], KeyType.Type));
                    ku.Add(MakeUnit(":", KeyType.Symbol));
                    if (sp[0] == "flag")
                    {
                        List<KeyUnit> ku1 = new();
                        try
                        {
                            var sp2 = new StringSplitEx(string.Join(":", sp[1..]), ':', 2);
                            ku1.Add(MakeUnit(sp2[0], KeyType.Tag));
                            ku1.Add(MakeUnit(":", KeyType.Symbol));
                            ku1.Add(MakeUnit(sp2[1], KeyType.String));
                        }
                        catch
                        {
                            ku1.Add(MakeUnit(string.Join("", sp[1..]), KeyType.Text));
                        }
                        ku.AddRange(ku1);
                    }
                    else if (!keys.ContainsKey(sp[0]))
                    {
                        ku.Add(MakeUnit(sp[1], KeyType.Text));
                    }
                    else if(keys[sp[0]].Type == KeyType.KnownType && 
                            keys[sp[0]].ConstantType.HasValue &&
                            keys[sp[0]].ConstantType.Value != KeyType.Unknown)
                    {
                        var ct = keys[sp[0]].ConstantType;
                        bool canparse = IsOnlyType(string.Concat(sp[1..]), _valueVaildRegularExpressions[ct.Value]);
                        if(canparse)
                        {
                            ku.Add(MakeUnit(sp[1], ct.Value));
                        }
                        else
                        {
                            ku.Add(MakeUnit(sp[1], KeyType.Text));
                        }
                    }
                    else
                    {
                        ku.Add(MakeUnit(sp[1], KeyType.Text));
                    }
                MiddleEnd:
                    if (!part.HasFlag(ParenthesisType.Half))
                        ku.Add(MakeUnit("]", KeyType.Parenthesis));
                    break;
                case ParenthesisType.Small:
                    ku.Add(MakeUnit("(", KeyType.Parenthesis));
                    var su1 = su[1..(part.HasFlag(ParenthesisType.Half) ? ^0 : ^1)];
                    if (su.Split("::").Length > 2)
                    {
                        var sp3 = new StringSplit(su1, "::");
                        if (keys.ContainsKey(sp3[0]) && keys[sp3[0]].Type == KeyType.CritialVariable)
                        {
                            ku.Add(keys[sp3[0]]);
                        }
                        else
                        {
                            ku.Add(MakeUnit(sp3[0], KeyType.Variable));
                        }

                        ku.Add(MakeUnit("::", KeyType.Expression));

                        if (keys.ContainsKey(sp3[1]) && keys[sp3[1]].Type == KeyType.KnownType)
                        {
                            ku.Add(keys[sp3[1]]);
                        }
                        else
                        {
                            ku.Add(MakeUnit(sp3[1], KeyType.Type));
                        }

                        ku.Add(MakeUnit("::", KeyType.Expression));

                        ku.Add(MakeUnit(sp3[2], KeyType.Property));
                    }
                    else if (keys.ContainsKey(su1) && keys[su1].Type == KeyType.CritialVariable)
                        ku.Add(MakeUnit(su1, KeyType.CritialVariable));
                    else
                        ku.Add(MakeUnit(su1, KeyType.Variable));
                    if (!part.HasFlag(ParenthesisType.Half))
                        ku.Add(MakeUnit(")", KeyType.Parenthesis));
                    break;
                default:
                    ku.Add(MakeUnit(su, KeyType.Text));
                    break;
            }
            ku.Add(MakeUnit(" ", KeyType.Split));
        }

        return ku;
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