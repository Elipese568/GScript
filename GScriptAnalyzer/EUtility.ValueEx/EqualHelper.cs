namespace EUtility.ValueEx;

public class EqualHelper
{
    public static bool TypeEqual(object obj1, object obj2)
    {
        Type t1 = obj1.GetType();
        Type t2 = obj2.GetType();
        return t1 == t2;
    }

    public static bool ValueEqual(object obj1, object obj2)
    {
        bool typeEqual = TypeEqual(obj1, obj2);
        if(!typeEqual)
            return false;

        return obj1.Equals(obj2);
    }

    public static bool CollectionLengthEqual<T>(IEnumerable<T> obj1, IEnumerable<T> obj2)
        => obj1.Count() == obj2.Count();

    public static bool CollectionItemsEqual<T>(IList<T> obj1, IList<T> obj2)
    {
        for(int i = 0; i < obj1.Count(); i++)
        {
            if (ValueEqual(obj1[i].As<object>(), obj2[i].As<object>()))
                return false;
        }
        return true;
    }

    public static bool CollectionEqual<T>(IList<T> obj1, IList<T> obj2)
    {
        bool lengthEqual = CollectionLengthEqual<T>(obj1, obj2);
        if(!lengthEqual)
            return false;

        bool itemsEqual = CollectionItemsEqual(obj1, obj2);
        if(!itemsEqual) 
            return false;

        return true;
    }

    public static bool CollectionEqual(object collect1, object collect2)
    {
        return CollectionEqual(collect1.As<ICollection<object>>(), collect2.As<ICollection<object>>());
    }
}
