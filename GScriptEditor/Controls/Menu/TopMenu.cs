using GScript.Editor.Controls.Menu.Primitives;
using System.Drawing;
using TrueColorConsole;

namespace GScript.Editor.Controls.Menu;

public class TopMenu
{
    public List<MenuGroup> Groups { get; set; }
    private int _selectedGroup;
    private readonly Dictionary<string, int> _groupLeftMap = new();

    public TopMenu(List<MenuGroup> groups)
    {
        Groups = groups;
        _selectedGroup = 0;
    }

    /// <summary>
    /// 显示菜单并等待用户选择，返回 MenuSelectedData
    /// </summary>
    public MenuSelectedData Ask(Action resumeBufferAction,int menuWidth = 16)
    {
        VTConsole.CursorAnsiSave();
        Console.CursorVisible = false;
        Console.SetCursorPosition(0, 0);

        while (true)
        {
            resumeBufferAction();
            RenderMenuBar();
            var key = Console.ReadKey(false);

            if (key.Key == ConsoleKey.LeftArrow)
            {
                _selectedGroup--;
                if (_selectedGroup < 0)
                    _selectedGroup = 0;
            }
            else if (key.Key == ConsoleKey.RightArrow)
            {
                _selectedGroup++;
                if (_selectedGroup >= Groups.Count)
                    _selectedGroup = Groups.Count - 1;
            }
            else if (key.Key == ConsoleKey.Enter)
            {
                var group = Groups[_selectedGroup];
                int left = _groupLeftMap[group.Name];
                int itemIdx = ShowSubMenu(group, left, menuWidth);
                Console.CursorVisible = true;
                VTConsole.CursorAnsiRestore();
                if (itemIdx >= 0)
                {
                    return new MenuSelectedData
                    {
                        GroupIndex = _selectedGroup,
                        ItemIndex = itemIdx,
                        Group = group,
                        Item = group.Items[itemIdx]
                    };
                }
            }
            else if (key.Key == ConsoleKey.Escape)
            {
                Console.CursorVisible = true;
                VTConsole.CursorAnsiRestore();
                return new MenuSelectedData
                {
                    GroupIndex = -1,
                    ItemIndex = -1,
                    Group = null,
                    Item = null
                };
            }
        }
    }

    private void RenderMenuBar()
    {
        Console.SetCursorPosition(0, 0);
        for (int i = 0; i < Console.WindowWidth; i++)
            Console.Write(' ');
        Console.SetCursorPosition(0, 0);

        for (int i = 0; i < Groups.Count; i++)
        {
            SetAccent(i == _selectedGroup);
            var group = Groups[i];
            if (!_groupLeftMap.ContainsKey(group.Name))
                _groupLeftMap.Add(group.Name, Console.CursorLeft);
            for (int j = 0; j < 8; j++)
            {
                if (j < group.Name.Length)
                    Console.Write(group.Name[j]);
                else
                    Console.Write(' ');
            }
            Console.ResetColor();
            Console.Write("  ");
        }
        Console.ResetColor();
    }

    private int ShowSubMenu(MenuGroup group, int left, int width)
    {
        int selectSub = 0;
        while (true)
        {
            RenderMenuItems(group, selectSub, left, width);
            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.UpArrow)
            {
                do
                {
                    selectSub--;
                    if (selectSub < 0)
                        selectSub = group.Items.Count -1;
                }
                while (!group.Items[selectSub].CanSelected());
            }
            else if (key.Key == ConsoleKey.DownArrow)
            {
                do
                {
                    selectSub++;
                    if (selectSub >= group.Items.Count)
                        selectSub = 0;
                }
                while(!group.Items[selectSub].CanSelected());
            }
            else if (key.Key == ConsoleKey.Enter)
            {
                if (group.Items[selectSub].CanSelected())
                {
                    group.Items[selectSub].Selected();
                    return selectSub;
                }
            }
            else if (key.Key == ConsoleKey.Escape)
                return -1;
        }
    }

    private void RenderMenuItems(MenuGroup group, int selectedSub, int start, int width)
    {
        Console.ResetColor();
        Console.SetCursorPosition(start, 1);

        for (int i = 0; i < group.Items.Count; i++)
        {
            Console.CursorLeft = start;
            Console.Write(new string(' ', width));
            Console.CursorTop++;
        }
        Console.ResetColor();
        Console.SetCursorPosition(start, 1);

        for (int j = 0; j < group.Items.Count; j++)
        {
            var item = group.Items[j];
            var mode = selectedSub == j ? FoucsMode.Selected : FoucsMode.NoSelected;
            item.Draw(new DrawData
            {
                Mode = mode,
                Size = new Rectangle(0, 0, width, 1)
            });
            Console.CursorLeft = start;
            Console.CursorTop++;
        }
        Console.ResetColor();
    }

    private void SetAccent(bool rev = false)
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
}
