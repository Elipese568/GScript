// AT:   Elipese
// Date: 2023/2/12

using System.Text;

namespace GScript.Analyzer.Util
{
    public class StringSplit
    {
        string[] m_splitunits;

        string m_rawstring;
        string m_split;

        public string[] SplitUnit => m_splitunits;

        public StringSplit(string str, char splitchar)
        {
            m_rawstring = str;
            m_split = splitchar.ToString();

            Stack<ParenthesisType> pars = new();

            List<string> units = new List<string>();
            StringBuilder sb = new();
            foreach(char c in str)
            {
                if(StrParenthesis.GetCharHalfParenthesisType(c) is ParenthesisType t &&
                   !t.HasFlag(ParenthesisType.Unknown))
                {
                    if(pars.TryPeek(out var peek) && (peek ^ ParenthesisType.Left) == (t ^ ParenthesisType.Right))
                    {
                        pars.Pop();
                    }
                    else
                    {
                        pars.Push(t);
                    }

                    sb.Append(c);
                }
                else
                {
                    if(c != splitchar)
                        sb.Append(c);
                    else
                    {
                        if(pars.Count != 0)
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
            m_rawstring = str;
            m_split = splitchar;
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
        public List<string> SplitUnit => m_splitunits;
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
