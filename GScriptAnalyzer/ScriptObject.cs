// AT:   Elipese
// Date: 2023/3/12
using EUtility.ValueEx;

namespace GScript.Analyzer
{
    public class ScriptObject
    {
        protected string m_name;
        protected object? m_value;
        protected Union<string, System.Type> m_type;

        //public string Name { get => m_name; set => m_name = value; };
        public object? Value
        {
            get { return m_value; }
            set
            {
                if(value != null)
                {
                    m_type |= value.GetType();
                }

                m_value = value;
            }
        }

        public Union<string, System.Type> ValueType
        {
            get
            {
                return m_type;
            }
            set
            {
                m_type = value;
            }
        }

        public readonly long NullValue = long.MinValue;

        public ScriptObject()
        {
            m_type = new(typeof(Object));
        }

        public T As<T>()
        {
            CheckValue();
            return (T)Convert.ChangeType(m_value,typeof(T));
        }

        protected void CheckValue()
        {
            if (m_value is long longv && longv == NullValue)
                throw new NullReferenceException("Value is NullValue");
        }

        public override string ToString()
        {
            CheckValue();
            return m_value.ToString();
        }

        public virtual string ToStringDescription()
        {
            CheckValue();
            return $"{{Value : {m_value}, Type : \"{m_type}\"}}";
        }
    }
}
