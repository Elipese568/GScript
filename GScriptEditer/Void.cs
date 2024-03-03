using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScriptEditer;

internal class Void
{
    public static Void MakeVoid()
        => new Void();

    private Void() { }
}

internal static class VoidExtension
{
    public static bool IsVoid(this object obj)
    {
        return obj is Void;
    }
}
