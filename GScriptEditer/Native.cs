using System.Runtime.InteropServices;



namespace GScript.Editer.Clipboard;

public static partial class Native
{
    [LibraryImport("GScript.Editer.Clipboard.dll", StringMarshalling = StringMarshalling.Utf8)]
    public static partial string GetCurrentClipboardContent();
}
