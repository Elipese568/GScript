using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScript.Standard;

internal class StringHelper
{
    static List<KeyValuePair<string, string>> _repMap = new()
    {
        new KeyValuePair<string, string>( "\\n", "\n" )
    };
    public static string ToCSString(string str)
    {
        foreach(var s in _repMap)
        {
            str = str.Replace(s.Key, s.Value);
        }
        return str;
    }
}
