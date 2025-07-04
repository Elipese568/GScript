namespace GScript.Editor.Controls.Menu.Primitives;

public struct MenuSelectedData
{
    public int GroupIndex { get; set; }
    public int ItemIndex { get; set; }
    public MenuGroup? Group { get; set; }
    public IMenuItem? Item { get; set; }
    public bool IsValid => GroupIndex >= 0 && ItemIndex >= 0 && Group != null && Item != null;
}
