using System.Drawing;
using TrueColorConsole;

namespace GScript.Editor.Controls.Menu.Primitives;

public enum FoucsMode
{
    NoSelected,
    Selected
}

public struct DrawData
{
    public FoucsMode Mode { get; set; }
    public Rectangle Size { get; set; }
}

public interface IMenuItem
{
    void Draw(DrawData data);
    bool CanSelected();
    void Selected();
}

public class TextMenuItem : IMenuItem
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
                if (i < leftpadding || i - leftpadding >= Text.Length)
                {
                    Console.Write(' ');
                    continue;
                }
                Console.Write(Text[i - leftpadding]);
            }
        }

        switch (data.Mode)
        {
            case FoucsMode.NoSelected: DrawInternal(CodeHighlightRuleSet._fore, CodeHighlightRuleSet._back, 1); break;
            case FoucsMode.Selected: DrawInternal(CodeHighlightRuleSet._back, CodeHighlightRuleSet._fore, 1); break;
        }
    }

    public bool CanSelected() => true;

    public void Selected()
    {
        SelectedEvent?.Invoke();
    }
}

public class SeparationItem : IMenuItem
{
    public void Draw(DrawData data)
    {
        VTConsole.SetColorForeground(CodeHighlightRuleSet._fore);
        VTConsole.SetColorBackground(CodeHighlightRuleSet._back);
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

public class MenuGroup
{
    public string Name { get; set; }
    public List<IMenuItem> Items { get; set; }
    public MenuGroup(string name, List<IMenuItem> items)
    {
        Name = name;
        Items = items;
    }
}
