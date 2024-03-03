namespace GScript.Standard;

public class DpObject
{
    private Dictionary<string, object> _properties;

    public void SetValue(string key, object value)
    {
        if(_properties.ContainsKey(key))
            _properties[key] = value;
        else
            _properties.Add(key, value);
    }

    public object GetValue(string key)
    {
        return _properties[key];
    }
}
