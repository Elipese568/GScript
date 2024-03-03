// AT:   Elipese
// Date: 2023/2/12

using System.Text;

namespace GScript.Analyzer.Util
{
    public class StringSplit
    {
        string[] m_splitunits;

        public string[] SplitUnit => m_splitunits;

        public StringSplit(string str, char splitchar)
        {
            bool inPars = false;

            List<string> units = new List<string>();
            StringBuilder sb = new();
            foreach(char c in str)
            {
                if (!StrParenthesis.GetStringParenthesisType(c.ToString()).HasFlag(ParenthesisType.Unknown))
                {
                    inPars = !inPars;
                    sb.Append(c);
                }
                else
                {
                    if(c != splitchar)
                        sb.Append(c);
                    else
                    {
                        if(inPars)
                            sb.Append(c);
                        else
                        {
                            units.Add(sb.ToString());
                            sb.Clear();
                        }
                    }
                }
            }

            if(sb.Length > 0)
            {
                units.Add(sb.ToString());
            }

            m_splitunits = units.ToArray();
        }

        public StringSplit(string str, string splitchar)
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

    public class StringSplitEx
    {
        List<string> m_splitunits = new();
        public StringSplitEx(string str, char splitchar, int depth)
        {
            int count = 0;
            int index = 0;
            StringBuilder current = new();
            foreach(char c in str)
            {
                if(c != splitchar)
                {
                    current.Append(c);
                }
                else
                {
                    count++;
                    bool canbr = false;
                    if (count == depth)
                    {
                        current.Append(splitchar + str[(index + 1)..]);
                        canbr = true;
                    }
                    m_splitunits.Add(current.ToString());
                    current = new();
                    if (canbr)
                        break;
                }
                index++;
            }
            m_splitunits.Add(current.ToString());
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
                return m_splitunits.ToArray()[range];
            }
        }
    }
}
