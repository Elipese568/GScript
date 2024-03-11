using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GScript.Editer;

internal struct ColorSet
{
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }

    public override int GetHashCode()
    {
        return R * G * B;
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj.GetHashCode() == GetHashCode();
    }

    public static implicit operator Color(ColorSet set)
    {
        return Color.FromArgb(set.R, set.G, set.B);
    }
}

internal class ColorStyle
{
    public ColorSet ForegroundColor { get; set; }
    public ColorSet BackgroundColor { get; set; }
}

internal class ColorStyleEx
{
    public Regex Rule { get; set; }
    public ColorSet ForegroundColor { get; set; }
    public ColorSet BackgroundColor { get; set; }
}

internal class StyleTable
{
    public string InterpreterPath { get; set; }
    public Dictionary<KeyType, ColorStyle> ColorStyle { get; set; } = new ();
    //public Dictionary<string, ColorStyleEx> CustomConstantColorRule { get; set; }

    public List<NormalKeyUnit> CritialVariable { get; set; } = new();
    public List<NormalKeyUnit> Control { get; set; } = new();
    public List<KnownTypeKeyUnit> KnownType { get; set; } = new();
    public List<NormalKeyUnit> Operator { get; set; } = new();
    public List<NormalKeyUnit> Tag { get; set; } = new();
    public List<NormalKeyUnit> Definition { get; set; } = new();
    public List<NormalKeyUnit> Special { get; set; } = new();

    public List<KeyUnit> GetList()
    {
        var list = new List<KeyUnit>();
        CritialVariable.ForEach(x => list.Add(new() { ConstantType = null, RawString = x.RawString, Type = x.Type }));
        Control.ForEach(x => list.Add(new() { ConstantType = null, RawString = x.RawString, Type = x.Type }));
        Operator.ForEach(x => list.Add(new() { ConstantType = null, RawString = x.RawString, Type = x.Type }));
        Tag.ForEach(x => list.Add(new() { ConstantType = null, RawString = x.RawString, Type = x.Type }));
        Definition.ForEach(x => list.Add(new() { ConstantType = null, RawString = x.RawString, Type = x.Type }));
        Special.ForEach(x => list.Add(new() { ConstantType = null, RawString = x.RawString, Type = x.Type }));
        KnownType.ForEach(x => list.Add(new() { ConstantType = x.ConstantType, RawString = x.RawString, Type = x.Type }));
        return list;
    }
}
