using System.Runtime.InteropServices;



namespace GScript.Editor.Clipboard;

public static partial class Native
{
    [LibraryImport("GScript.Editor.Clipboard.dll", StringMarshalling = StringMarshalling.Utf8)]
    public static partial string GetCurrentClipboardContent();
}
