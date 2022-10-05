using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.JsonConverters;
using Newtonsoft.Json;

namespace Common;

[JsonConverter(typeof(DepJsonConverter))]
public sealed class Dep : IEquatable<Dep>
{
    private static readonly ConcurrentDictionary<string, string> DepDefaultConfigurationCache = new();

    public Dep(string name, string treeish = null, string configuration = null)
    {
        Name = name;
        Treeish = treeish;
        Configuration = configuration;
    }

    public Dep(string fromYamlString)
    {
        var tokens = new List<string>();
        var currentToken = "";
        fromYamlString += "@";
        for (var pos = 0; pos < fromYamlString.Length; pos++)
        {
            if ((fromYamlString[pos] == '/' || fromYamlString[pos] == '@') &&
                (pos == 0 || fromYamlString[pos - 1] != '\\'))
            {
                tokens.Add(currentToken);
                currentToken = fromYamlString[pos].ToString();
            }
            else
                currentToken += fromYamlString[pos];
        }

        Name = tokens[0];
        foreach (var token in tokens.Select(UnEscapeBadChars))
        {
            if (token.StartsWith("@"))
                Treeish = token.Substring(1);
            if (token.StartsWith("/"))
                Configuration = token.Substring(1);
        }

        if (Treeish == "")
            Treeish = null;
        if (Configuration == "")
            Configuration = null;
    }

    public string Name { get; }
    public string Treeish { get; set; }
    public string Configuration { get; set; }

    public void UpdateConfigurationIfNull()
    {
        UpdateConfigurationIfNull(Helper.CurrentWorkspace);
    }

    public void UpdateConfigurationIfNull(string workspace)
    {
        if (!string.IsNullOrEmpty(Configuration)) return;
        var path = Path.Combine(workspace, Name);
        if (!DepDefaultConfigurationCache.ContainsKey(path))
        {
            DepDefaultConfigurationCache[path] =
                new ConfigurationParser(new FileInfo(Path.Combine(workspace, Name)))
                    .GetDefaultConfigurationName();
        }

        Configuration = DepDefaultConfigurationCache[path];
    }

    public bool Equals(Dep dep)
    {
        if (dep == null)
            return false;
        return Name == dep.Name && Treeish == dep.Treeish && Configuration == dep.Configuration;
    }

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }

    public override string ToString()
    {
        return Name +
               (string.IsNullOrEmpty(Configuration) ? "" : Helper.ConfigurationDelimiter + Configuration) +
               (string.IsNullOrEmpty(Treeish) ? "" : "@" + Treeish);
    }

    public string ToYamlString()
    {
        return Name +
               (string.IsNullOrEmpty(Configuration) ? "" : Helper.ConfigurationDelimiter + Configuration) +
               (string.IsNullOrEmpty(Treeish) ? "" : "@" + Treeish.Replace("@", "\\@").Replace("/", "\\/"));
    }

    public string ToBuildString()
    {
        return Name +
               (Configuration == null || Configuration.Equals("full-build") ? "" : "/" + Configuration);
    }

    private string UnEscapeBadChars(string str)
    {
        return str.Replace("\\@", "@").Replace("\\/", "/");
    }
}
