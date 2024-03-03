using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScript.Analyzer
{
    public class GSVarListItem
    {
        internal GSVarListItem? m_next;
        internal GSVarListItem? m_prev;
        internal Variable? m_var;

        public GSVarListItem? Next => m_next;
        public GSVarListItem? Prev => m_prev;
        public Variable? Value => m_var;
    }
}
