using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScriptAnalyzer
{
    public class GSVar : GSObject
    {
        private string m_name;

        public string Name { get { return m_name; } set { m_name = value; } }

        public GSVar() : base() => m_name = "";

        public override string ToString()
        {
            CheckValue();
            return $"{{Name : \"{m_value}\", Value : {m_value}, Type : \"{m_type}\"}}";
        }
    }
}
