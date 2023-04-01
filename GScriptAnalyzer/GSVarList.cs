using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScriptAnalyzer
{
    public class GSVarList : IEnumerator<GSVarListItem>
    {
        private GSVarListItem? m_current;

        internal GSVarListItem? m_start;
        internal GSVarListItem? m_end;

        public GSVarListItem? Start => m_start;
        public GSVarListItem? End => m_end;

        GSVarListItem IEnumerator<GSVarListItem>.Current => m_current;

        object? IEnumerator.Current => m_current;

        public GSVarList()
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

        public void AddToEnd(GSVar item)
        {
            var e = new GSVarListItem();
            e.m_prev = End;
            e.m_next = null;
            e.m_var = item;
            m_end = e;
        }

        public void AddToStart(GSVar item)
        {
            var e = new GSVarListItem();
            e.m_prev = null;
            e.m_next = Start;
            e.m_var = item;
            m_start = e;
        }

        public GSVar RemoveEnd()
        {
            var e = End.Prev;
            m_end = e;
            return e.Value;
        }

        public GSVar RemoveStart()
        {
            var e = Start.Prev;
            m_start = e;
            return e.Value;
        }

        private ref GSVarListItem GetIndexItemRef(int index)
        {
            ref var cur = ref m_start;
            for(int i = 0; i <=index;i++)
            {
                cur = ref cur.m_next;
            }
            return ref cur;
        }

        private GSVarListItem GetIndexItemVal(int index)
        {
            var cur = m_start;
            for (int i = 0; i <= index; i++)
            {
                cur = cur.m_next;
            }
            return cur;
        }

        public void InsertForce(int index,GSVar v)
        {
            var t = GetIndexItemVal(index);
            var it = new GSVarListItem { m_var = v, m_prev = t, m_next = GetIndexItemRef(index + 1) };
            GetIndexItemRef(index).m_next = it;
            GetIndexItemRef(index + 1).m_prev = it;
        }
        public void InsertBack(int index, GSVar v)
        {
            var t = GetIndexItemVal(index);
            var it = new GSVarListItem { m_var = v, m_prev = GetIndexItemRef(index-1), m_next = t };
            GetIndexItemRef(index-1).m_next = it;
            GetIndexItemRef(index).m_prev = it;
        }

        public GSVarListItem this[int index]
        {
            get
            {
                return GetIndexItemVal(index);
            }
        }

        public GSVarListItem this[string varstr]
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
