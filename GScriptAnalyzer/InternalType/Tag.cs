using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScript.Analyzer.InternalType;

public class Tag : ScriptObject
{
    public string TagName { get; set; }

    public Tag(string tagName)
    {
        TagName = tagName;
        Value = this;
        ValueType = typeof(Tag);
    }

    public override string ToString()
    {
        return TagName;
    }
}
