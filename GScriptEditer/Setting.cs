using GScript.Analyzer.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GScriptEditer;

internal class Setting
{
    private class SettingFile : IDisposable
    {
        private Dictionary<string, string> _map = new();
        private string _path = string.Empty;

        public SettingFile(string path)
        {
            _path = path;
            if (!File.Exists(path))
            {
                File.Create(path).Close();
                return;
            }

            string[] content = File.ReadAllLines(path);

            foreach(string l in content)
            {
                if (!l.Contains(':'))
                    throw new FormatException($"Format of this setting file {path} isn't right.");
                else if (l.Split(':') is string[] split && _map.ContainsKey(split[0]))
                    throw new InvalidDataException($"This key of {l.Split(':')[0]} is repeated.");
                else
                {
                    var splitex = new StringSplitEx(l, ':', 2);
                    _map.Add(splitex[0], splitex[1]);
                }
            }
        }

        public string this[string key]
        {
            get
            {
                if (!_map.ContainsKey(key))
                    throw new KeyNotFoundException(@$"Setting key ""{key}"" not found.");

                return _map[key];
            }
            set
            {
                if(_map.ContainsKey(key))
                    _map[key] = value;
                else
                    _map.Add(key, value);
            }
        }

        public bool Contains(string key)
            => _map.ContainsKey(key);

        public void Dispose()
        {
            StreamWriter sw = new(_path);
            foreach (var item in _map)
            {
                sw.Write(item.Key);
                sw.Write(':');
                sw.Write(item.Value);
            }
            sw.Close();
        }
    }

    public const string SettingFile_Name = "Setting.json"; // Can change

    static SettingFile _file;

    static Setting()
    {
        _file = new(SettingFile_Name);
        AppDomain.CurrentDomain.ProcessExit += (s, a) =>
        {
            _file.Dispose();
        };
    }

    public static void Init() { /* It has no content, but can initialize this :P  */ }

    public static void Save(string key, string value)
        => _file[key] = value;

    public static string Read(string key)
        => _file[key];

    public static bool Exists(string key)
        => _file.Contains(key);
}
