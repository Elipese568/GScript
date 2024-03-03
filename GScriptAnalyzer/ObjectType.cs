using EUtility.ValueEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GScript.Analyzer
{
    public class ObjectType : ScriptObject
    {
        public bool Exists { get; set; }

        public ObjectType(System.Type type) : base()
        {
            Value = type;
            ValueType = typeof(ObjectType);
            Exists = true;
        }

        public ObjectType(string typename) : base()
        {
            Value = typename;
            ValueType = typeof(ObjectType);
            Exists = false;
        }

        public static bool operator ==(ObjectType left, Union<string, Type> right)
        {
            if (left.Exists && right.GetValue<Type>() != null)
            {
                return left.Value as Type == right.GetValue<Type>();
            }
            else if (left.Exists && right.GetValue<Type>() == null)
            {
                return false;
            }
            else if (!left.Exists && right.GetValue<string>() != null)
            {
                return left.Value as string == right.GetValueT1();
            }
            else if (!left.Exists && right.GetValue<string>() == null)
            {
                return false;
            }
            return false;
        }

        public static bool operator !=(ObjectType left, Union<string, Type> right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }
}
