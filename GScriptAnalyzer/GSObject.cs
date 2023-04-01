// AT:   Elipese
// Date: 2023/3/12
namespace GScriptAnalyzer
{
    public class GSObject
    {
        protected string m_name;
        protected object? m_value;
        protected Type m_type;

        //public string Name { get => m_name; set => m_name = value; };
        public object? Value
        {
            get { return m_value; }
            set
            {
                m_type = value.GetType();
                m_value = value;
            }
        }

        public Type ValueType => m_type;

        public readonly long NullValue = long.MinValue;

        public GSObject()
        {
            m_type = typeof(object);
        }

        public T As<T>()
        {
            CheckValue();
            return (T)Convert.ChangeType(m_value,typeof(T));
        }

        protected void CheckValue()
        {
            if ((long)m_value == NullValue)
                throw new NullReferenceException("Value is NullValue");
        }

        public override string ToString()
        {
            CheckValue();
            return $"{{Value : {m_value}, Type : \"{m_type}\"}}";
        }
    }
}
