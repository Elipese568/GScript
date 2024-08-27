using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScript.Analyzer.InternalType
{
    public class Variable : ScriptObject
    {
        private string m_name;

        public string Name { get { return m_name; } set { m_name = value; } }

        public Variable() : base() => m_name = "";
        public Variable(string name) : base() => m_name = name;

        public override string ToString()
        {
            CheckValue();
            return $"{{Name : \"{m_value}\", Value : {m_value}, Type : \"{m_type}\"}}";
        }

        public override string ToStringDescription()
        {
            CheckValue();
            return $"{{Name : \"{m_value}\", Value : {m_value}, Type : \"{m_type}\"}}";
        }
    }
}
