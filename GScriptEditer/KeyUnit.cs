using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScript.Editer;

internal struct KeyUnit
{
    public KeyType Type { get; set; }
    public string RawString { get; set; }
    public KeyType? ConstantType { get; set; }
}

internal struct KnownTypeKeyUnit
{
    public KeyType Type { get; set; }
    public string RawString { get; set; }
    public KeyType ConstantType { get; set; }
}

internal struct NormalKeyUnit
{
    public KeyType Type { get; set; }
    public string RawString { get; set; }
}