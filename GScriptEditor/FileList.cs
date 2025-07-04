using EUtility;
using EUtility.ConsoleEx.Message;
using EUtility.StringEx.StringExtension;
using EUtility.ValueEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Void = EUtility.ValueEx.Void;

namespace GScript.Editor;

public static class Menu
{
    static MessageOutputer menuguide = new()
        {
            new MessageUnit()
            {
                Title = "↑",
                Description = "上一个选项"
            },
            new MessageUnit()
            {
                Title = "↓",
                Description = "下一个选项"
            },
            new MessageUnit()
            {
                Title = "Enter",
                Description = "确认选项"
            }
        };
    static Menu()
    {

    }

    public static int WriteLargerMenu(Dictionary<string, string> menuitem, int maxitems = 8, int curstartindex = 2)
    {
        int select = 0, startitem = 0, enditem = maxitems;
        void WriteItems()
        {
            int index = startitem;
            foreach (var item in menuitem.Skip(startitem).SkipLast(menuitem.Count - startitem - enditem))
            {
                if (index == select)
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.WriteLine("   -->" + item.Key + new string(' ', Console.WindowWidth - (item.Key + "   -->").GetStringInConsoleGridWidth()));
                    Console.ResetColor();

                    var ct = Console.CursorTop;
                    Console.CursorTop = maxitems + 2 + curstartindex;
                    Console.WriteLine("说明：");
                    var dct = Console.CursorTop;
                    for (int i = 0; i < Console.WindowHeight - Console.CursorTop - 1; i++)
                    {
                        Console.WriteLine(new string(' ', Console.WindowWidth));
                    }
                    Console.CursorTop = dct;
                    Console.WriteLine(item.Value);
                    Console.CursorTop = ct;
                }
                else
                {
                    Console.WriteLine(item.Key + new string(' ', Console.WindowWidth - item.Key.GetStringInConsoleGridWidth()));
                }
                index++;
            }
        }


        while (true)
        {
            menuguide.Write();
            Console.CursorTop = curstartindex;
            WriteItems();

            Console.CursorVisible = false;
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.UpArrow)
            {
                select--;
                if (select < 0)
                    select = 0;
                if (select < startitem)
                {
                    startitem = select;
                    enditem--;
                    if (startitem < 0)
                    {
                        startitem = menuitem.Count - enditem;
                        enditem = menuitem.Count - 1;
                        select = enditem;
                    }

                }
            }
            else if (key.Key == ConsoleKey.DownArrow)
            {
                select++;
                if (select > menuitem.Count - 1)
                    select = menuitem.Count - 1;
                if (select > enditem)
                {
                    enditem = select;
                    startitem++;
                    if (enditem > menuitem.Count)
                    {
                        enditem = maxitems;
                        select = 0;
                        startitem = 0;
                    }
                }
            }
            else if (key.Key == ConsoleKey.Enter)
            {
                return select;
            }
        }
    }
}

internal class FileList
{
    public static bool IsDir(string path) => File.GetAttributes(path).HasFlag(FileAttributes.Directory);

    public static Union<Void, FileInfo> FileDialog(string path)
    {
        if (Setting.Exists("FileList_OpenPath") && Path.Exists(Setting.Read("FileList_OpenPath")))
            return _FileDialog(Setting.Read("FileList_OpenPath"));

        return _FileDialog(path);
    }

    public static Union<Void, FileInfo> _FileDialog(string? path)
    {
        Console.Clear();
        MessageOutputer menuguide = new()
        {
            new MessageUnit()
            {
                Title = "↑",
                Description = "上一个选项"
            },
            new MessageUnit()
            {
                Title = "↓",
                Description = "下一个选项"
            },
            new MessageUnit()
            {
                Title = "Enter",
                Description = "确认选项"
            },
            new MessageUnit()
            {
                Title = "Esc",
                Description = "退出列表"
            }
        };
        List<string> items;
        Dictionary<string, string> menuitems;
        Console.WriteLine("请选择文件");
        int select = 0, startitem = 0, enditem = Console.WindowHeight / 3 * 2;

        void WriteItems()
        {
            int index = startitem;
            Refresh(ref path, out items, out menuitems);
            foreach (var item in menuitems.ToArray()[startitem..Math.Min(menuitems.Count, enditem)])
            {
                if (index == select)
                {
                    Console.BackgroundColor = ConsoleColor.White;
                    Console.ForegroundColor = ConsoleColor.Black;
                    var l = Console.WindowWidth - (item.Key + "   -->").GetStringInConsoleGridWidth();
                    Console.WriteLine("   -->" + (l < 0 ? item.Key[..(Console.WindowWidth - 6)] : item.Key) + new string(' ', l < 0 ? 0 : l));
                    Console.ResetColor();

                    var ct = Console.CursorTop;
                    Console.CursorTop = Console.WindowHeight / 3 * 2 + 2 + 2;
                    Console.WriteLine("说明：");
                    var dct = Console.CursorTop;
                    for (int i = 0; i < Console.WindowHeight - Console.CursorTop - 1; i++)
                    {
                        Console.WriteLine(new string(' ', Console.WindowWidth));
                    }
                    Console.CursorTop = dct;
                    Console.WriteLine(item.Value);
                    Console.CursorTop = ct;
                }
                else
                {
                    var l = Console.WindowWidth - item.Key.GetStringInConsoleGridWidth();
                    Console.WriteLine(l < 0 ? item.Key[..Console.WindowWidth] : item.Key + new string(' ', l < 0 ? 0 : l));
                }
                index++;
            }
        }


        while (true)
        {
            menuguide.Write();
            Console.CursorTop = 2;
            WriteItems();

            Console.CursorVisible = false;
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape)
                return Void.MakeVoid();

            if (key.Key == ConsoleKey.UpArrow)
            {
                select--;
                if (select < 0)
                    select = 0;
                if (select < startitem)
                {
                    startitem = select;
                    enditem--;
                    if (startitem < 0)
                    {
                        startitem = 0;
                        enditem++;
                        select = 0;
                    }

                }
            }
            else if (key.Key == ConsoleKey.DownArrow)
            {
                select++;
                if (select > menuitems.Count - 1)
                    select = menuitems.Count - 1;
                if (select >= enditem)
                {
                    enditem++;
                    startitem++;
                    if (enditem >= menuitems.Count)
                    {
                        enditem--;
                        select--;
                        startitem = menuitems.Count - 1 - Console.WindowHeight / 3 * 2;
                    }
                }
            }
            else if (key.Key == ConsoleKey.F1)
            {
                Console.Clear();
                Console.WriteLine("请选择操作");
                int selecta = Menu.WriteLargerMenu(new()
                {
                    { "新建文件", "在当前目录下新建一个文件" },
                    { "新建文件夹", "在当前目录下新建一个文件夹" }
                });
                Console.Clear();
                Console.WriteLine("请输入名称\n");
                string name = Console.ReadLine();
                if (selecta == 0)
                {
                    File.Create(Path.Combine(path, name)).Close();
                }
                else
                {
                    Directory.CreateDirectory(Path.Combine(path, name));
                }
                continue;
            }
            else if (key.Key == ConsoleKey.Enter)
            {
                break;
            }
        }
        if (items[select] == "..")
        {
            var d = new DirectoryInfo(path);
            if (d.Root.FullName == d.FullName)
            {
                var drs = DriveInfo.GetDrives().ToList();
                Dictionary<string, string> drMap = new();
                drs.ForEach(x => drMap.Add(x.VolumeLabel, x.Name));
                var drselect = Menu.WriteLargerMenu(drMap);
                return _FileDialog(drs[drselect].Name);
            }
            return _FileDialog(d.Parent.FullName);
        }
        if (IsDir(items[select]))
        {
            return _FileDialog(items[select]);
        }
        else
        {
            Setting.Save("FileList_OpenPath", path);
            return new FileInfo(items[select]);
        }

        static void Refresh(ref string path, out List<string> items, out Dictionary<string, string> menuitems)
        {
            try
            {
                items = Directory.GetFileSystemEntries(path).ToList();
                items.Insert(0, "..");
                menuitems = new();
                foreach (var item in items)
                {
                    menuitems.Add(item, item);
                }
            }
            catch (UnauthorizedAccessException)
            {
                path = new DirectoryInfo(path).Parent.FullName;
                Native.MessageBoxA(new IntPtr(0), "Unauthorized Access.", "Error", 0x10);
                items = Directory.GetFileSystemEntries(path).ToList();
                items.Insert(0, "..");
                menuitems = new();
                foreach (var item in items)
                {
                    menuitems.Add(item, item);
                }
            }

        }
    }
}
