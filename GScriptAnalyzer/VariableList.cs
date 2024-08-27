using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GScript.Analyzer.InternalType;

namespace GScript.Analyzer
{
    [Obsolete("This planed **Feature** is obsoleting.", true)]
    public class VariableList : IEnumerator<VariableListItem>
    {
        private VariableListItem? m_current;

        internal VariableListItem? m_start;
        internal VariableListItem? m_end;

        public VariableListItem? Start => m_start;
        public VariableListItem? End => m_end;

        VariableListItem IEnumerator<VariableListItem>.Current => m_current;

        object? IEnumerator.Current => m_current;

        public VariableList()
        {
            m_start = null;
            m_start = null;
        }

        void IDisposable.Dispose()
        {
            try
            {
                while(true)
                {
                    if (End == null)
                        break;
                    End.m_var = null;
                    m_end = End.m_prev;
                }
            }
            catch { }
            return;
        }

        bool IEnumerator.MoveNext()
        {
            return (m_current = Start ?? null) == null;
        }

        void IEnumerator.Reset()
        {
            m_current = Start;
        }

        public void AddToEnd(Variable item)
        {
            var e = new VariableListItem();
            e.m_prev = End;
            e.m_next = null;
            e.m_var = item;
            m_end = e;
        }

        public void AddToStart(Variable item)
        {
            var e = new VariableListItem();
            e.m_prev = null;
            e.m_next = Start;
            e.m_var = item;
            m_start = e;
        }

        public Variable RemoveEnd()
        {
            var e = End.Prev;
            m_end = e;
            return e.Value;
        }

        public Variable RemoveStart()
        {
            var e = Start.Prev;
            m_start = e;
            return e.Value;
        }

        private ref VariableListItem GetIndexItemRef(int index)
        {
            ref var cur = ref m_start;
            for(int i = 0; i <=index;i++)
            {
                cur = ref cur.m_next;
            }
            return ref cur;
        }

        private VariableListItem GetIndexItemVal(int index)
        {
            var cur = m_start;
            for (int i = 0; i <= index; i++)
            {
                cur = cur.m_next;
            }
            return cur;
        }

        public void InsertForce(int index,Variable v)
        {
            var t = GetIndexItemVal(index);
            var it = new VariableListItem { m_var = v, m_prev = t, m_next = GetIndexItemRef(index + 1) };
            GetIndexItemRef(index).m_next = it;
            GetIndexItemRef(index + 1).m_prev = it;
        }
        public void InsertBack(int index, Variable v)
        {
            var t = GetIndexItemVal(index);
            var it = new VariableListItem { m_var = v, m_prev = GetIndexItemRef(index-1), m_next = t };
            GetIndexItemRef(index-1).m_next = it;
            GetIndexItemRef(index).m_prev = it;
        }

        public VariableListItem this[int index]
        {
            get
            {
                return GetIndexItemVal(index);
            }
        }

        public VariableListItem this[string varstr]
        {
            get
            {
                var t = m_start;
                while(true)
                {
                    if(t.Value.Name == varstr)
                    {
                        return t;
                    }
                    t = t.m_next;
                }
                throw new KeyNotFoundException();
            }
        }
    }
}
