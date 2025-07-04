using System.Runtime.InteropServices;



namespace GScript.Editor;

public static partial class Native
{
    [LibraryImport("GScript.Editor.Clipboard.dll", StringMarshalling = StringMarshalling.Utf8)]
    public static partial string GetCurrentClipboardContent();

    [DllImport("User32.dll")]
    public extern static int MessageBoxA(nint ptr, string message, string caption, uint flag);
}
