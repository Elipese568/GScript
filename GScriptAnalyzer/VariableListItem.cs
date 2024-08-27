using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GScript.Analyzer.InternalType;

namespace GScript.Analyzer
{
    [Obsolete("This planed **Feature** is obsoleting.", true)]
    public class VariableListItem
    {
        internal VariableListItem? m_next;
        internal VariableListItem? m_prev;
        internal Variable? m_var;

        public VariableListItem? Next => m_next;
        public VariableListItem? Prev => m_prev;
        public Variable? Value => m_var;
    }
}
