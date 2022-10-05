using System.Collections.Generic;
using System.Linq;

namespace Common;

public sealed class IniData
{
    private readonly Dictionary<string, Dictionary<string, string>> data;

    public IniData(Dictionary<string, Dictionary<string, string>> data)
    {
        this.data = data;
    }

    public string GetValue(string key, string section)
    {
        return GetValue(key, section, "");
    }

    public string[] GetKeys(string section)
    {
        if (!data.ContainsKey(section))
            return new string[0];

        return data[section].Keys.ToArray();
    }

    public string[] GetSections()
    {
        return data.Keys.Where(t => t != "").ToArray();
    }

    private string GetValue(string key, string section, string @default)
    {
        if (!data.ContainsKey(section))
            return @default;

        if (!data[section].ContainsKey(key))
            return @default;

        return data[section][key];
    }
}
