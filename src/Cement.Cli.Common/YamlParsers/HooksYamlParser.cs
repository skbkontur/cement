using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cement.Cli.Common.Exceptions;

namespace Cement.Cli.Common.YamlParsers;

public sealed class HooksYamlParser : ConfigurationYamlParser
{
    private const string SectionName = "hooks";

    public HooksYamlParser(FileInfo moduleName)
        : base(moduleName)
    {
    }

    public HooksYamlParser(string moduleName, string text)
        : base(moduleName, text)
    {
    }

    public List<string> Get()
    {
        try
        {
            var configDict = GetConfigurationSection("default");
            return GetHooksFromSection(configDict);
        }
        catch (BadYamlException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new BadYamlException(ModuleName, SectionName, exception.Message);
        }
    }

    private static List<string> GetHooksFromSection(Dictionary<string, object> configDict)
    {
        if (configDict == null || !configDict.ContainsKey(SectionName))
            return new List<string>();
        if (configDict[SectionName] is not List<object> hooks)
            return new List<string>();

        return hooks.Select(h => h.ToString()).ToList();
    }
}
