// AT:   Elipese
// Date: 2023/2/12

namespace GScriptAnalyzer.Util
{
    public class StringSplit
    {
        string[] m_splitunits;

        public string[] SplitUnit => m_splitunits;

        public StringSplit(string str, char splitchar)
        {
            m_splitunits = str.Split(splitchar);
        }

        public string this[int index]
        {
            get
            {
                return m_splitunits[index];
            }
        }

        public string[] this[Range range]
        {
            get
            {
                return m_splitunits[range];
            }
        }
    }
}
