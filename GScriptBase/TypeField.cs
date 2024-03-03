using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GScript.Standard;

internal static class TypeField
{
    public static readonly Type Int = typeof(int);
    public static readonly Type Long = typeof(long);
    public static readonly Type Float = typeof(float);
    public static readonly Type Double = typeof(double);
    public static readonly Type Boolean = typeof(bool);
    public static readonly Type String = typeof(string);
    public static readonly Type Type = typeof(Type);
    public static readonly Type Char = typeof(char);
    public static readonly Type Object = typeof(object);

    public static class Numeric
    {
        public static readonly Type Int128 = typeof(Int128);
        public static readonly Type BigInteger = typeof(BigInteger);
    }
}
