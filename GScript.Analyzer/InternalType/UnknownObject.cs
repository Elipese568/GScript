using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace GScript.Analyzer.InternalType;

public class Unknown
{
    private string m_value = string.Empty;
    private string m_type = string.Empty;

    public static implicit operator string(Unknown value)
    {
        return value.m_value;
    }

    public Unknown(string value, string type)
    {
        m_value = value;
        m_type = type;
    }

    public override string ToString()
    {
        return m_value;
    }
}
