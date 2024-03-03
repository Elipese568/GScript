using GScript.Analyzer;

namespace GScript.Standard;

public class ClassProperty
{
    public string Name { get; set; }
    public Type Type { get; init; }

    public ClassProperty(string name, Type type)
    {
        Name = name;
        Type = type;
    }
}

public class ClassPropertyInstance
{
    public string Name { get; set; }
    public Type Type { get; init; }
    public object Value { get; private set; }

    public ClassPropertyInstance(ClassProperty @base)
    {
        Name = @base.Name;
        Type = @base.Type;
    }

    public bool SetValue(object value)
    {
        if (value == null)
            return false;
        if(value.GetType().Equals(Type))
        {
            Value = value;
            return true;
        }
        else
        {
            return false;
        }
    }
}

public enum Privation
{
    Private,
    Public
}

public class ClassFunction : ClassProperty
{
    public Privation Privation { get; set; }
    public int Entry { get; set; }

    public ClassFunction(string name, Privation privation, int entry) : base(name, typeof(void))
    {
        Entry = entry;
        Name = name;
        Privation = privation;
    }
}

public class ClassTemplate
{
    private Dictionary<string, ClassProperty> _prop = new Dictionary<string, ClassProperty>();
    private Dictionary<string, ClassFunction> _func = new Dictionary<string, ClassFunction>();

    public string Name
    {
        get;
        set;
    }

    public Range ClassArea
    {
        get;
        set;
    }

    public ClassTemplate(string name, int line)
    {
        Name = name;
        ClassArea = new(line, line + 1);
    }

    public void RegisterProp(string name, ClassProperty prop)
    {
        _prop.Add(name, prop);
    }

    public void RegisterFunc(string name, ClassFunction func)
    {
        _func.Add(name, func);
    }

    public void ClassEnd(int line)
    {
        ClassArea = new(ClassArea.Start, line);
    }

    public Dictionary<string, ClassProperty> GetPropertys() => _prop;
    public Dictionary<string, ClassFunction> GetFunctions() => _func;

    public ClassInstance CreateInstance(string name)
    {
        return new(this, name);
    }
}

public class ClassInstance
{
    private Dictionary<string, ClassPropertyInstance> _prop = new();
    private Dictionary<string, ClassFunction> _func = new();
    private ClassTemplate _ct;
    public string ClassName
    {
        get;
        init;
    }

    public string InstanceName
    {
        get;
        init;
    }

    public ClassInstance(ClassTemplate ct, string name)
    {
        foreach(var prop in ct.GetPropertys())
        {
            _prop.Add(prop.Key, new(prop.Value));
        }
        _func = ct.GetFunctions();

        ClassName = ct.Name;
        InstanceName = name;

        _ct = ct;
    }

    public bool SetProperty(string name, object value)
    {
        return _prop[name].SetValue(value);
    }

    public object GetProperty(string name)
    {
        return _prop[name].Value;
    }

    public bool Invoke(string name, ref int line, object[]? args)
    {
        if(!_func.ContainsKey(name))
        {
            ErrorData ed = new(line, "", new ArgumentException("Member function not exist."), "Member function not exist.");
            ExceptionOperator.SetLastErrorEx(ed);
            return false;
        }
        var func = _func[name];

        if(func.Privation == Privation.Private && !(_ct.ClassArea.Start.Value < line) && !(line < _ct.ClassArea.End.Value))
        {
            ErrorData ed = new(line, "", new ArgumentException("Member function + " + name + " it's not public."), "Member function + " + name + " it's not public.");
            ExceptionOperator.SetLastErrorEx(ed);
            return false;
        }

        Script.CurrentScript.SetVar(Entry.M_SelfObject, this);

        int i = 0;
        foreach(var arg in args ?? Array.Empty<object>())
        {
            Script.CurrentScript.SetVar($"$ARG{(i == 0 ? "" : i)}$", arg);
            i++;
        }

        line = func.Entry;
        return true;
    }
}
