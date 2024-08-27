using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GScript.Standard;

public class ModuleItem
{
    public string Name { get; set; }
    public string Description { get; set; }

    public string Path { get; set; }
}

public class Root
{
    public List<ModuleItem> Modules { get; set; }
}

public class ModuleMamager
{
    static Root _root;
    static ModuleMamager()
    {
        _root = JsonSerializer.Deserialize<Root>(File.ReadAllText("ModulesManifist.json"));
    }

    public static string GetFormattedModules()
    {
        StringBuilder sb = new();
        foreach (var module in _root.Modules)
        {
            sb.Append(module.Name)
              .Append(':')
              .AppendLine()
              .Append('\t')
              .Append("Description: ")
              .Append(module.Description)
              .Append('\t')
              .Append("Path: ")
              .Append(module.Path)
              .AppendLine();
        }
        return sb.ToString();
    }

    public static string GetModulePath(string name)
    {
        return _root.Modules.Find(x => x.Name == name).Path;
    }

    public static bool RegisterModule(string name, string des, string path)
    {
        if (_root.Modules.Exists(x => x.Name == name))
            return false;

        _root.Modules.Add(new()
        {
            Name = name,
            Description = des,
            Path = path
        });
        return true;
    }

    public static bool UnRegisterModule(string name)
    {
        if (!_root.Modules.Exists(x => x.Name == name))
            return false;

        _root.Modules.RemoveAll(x => x.Name == name);
        return true;
    }
}
