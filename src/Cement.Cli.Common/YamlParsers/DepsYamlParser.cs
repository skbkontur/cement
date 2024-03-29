using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cement.Cli.Common.DepsValidators;
using Cement.Cli.Common.Exceptions;

namespace Cement.Cli.Common.YamlParsers;

public sealed class DepsYamlParser : ConfigurationYamlParser
{
    private readonly ConsoleWriter consoleWriter;
    private readonly IDepsValidatorFactory depsValidatorFactory;

    public DepsYamlParser(ConsoleWriter consoleWriter, IDepsValidatorFactory depsValidatorFactory, FileInfo moduleName)
        : base(moduleName)
    {
        this.consoleWriter = consoleWriter;
        this.depsValidatorFactory = depsValidatorFactory;
    }

    public DepsYamlParser(ConsoleWriter consoleWriter, IDepsValidatorFactory depsValidatorFactory, string moduleName,
                          string contents)
        : base(moduleName, contents)
    {
        this.consoleWriter = consoleWriter;
        this.depsValidatorFactory = depsValidatorFactory;
    }

    public DepsData Get(string configuration = null)
    {
        configuration ??= GetDefaultConfigurationName();
        if (!ConfigurationExists(configuration))
        {
            consoleWriter.WriteWarning(
                $"Configuration '{configuration}' was not found in {ModuleName}. Will take full-build config");
            if (!ConfigurationExists("full-build"))
                throw new NoSuchConfigurationException(ModuleName, "full-build and " + configuration);
            configuration = "full-build";
        }

        var defaultDepsContent = ConfigurationExists("default")
            ? GetDepsContent("default")
            : new DepsData(null, new List<Dep>());
        var configDepsContent = GetDepsContent(configuration);
        var force = configDepsContent.Force ?? defaultDepsContent.Force;
        var deps = defaultDepsContent.Deps;
        deps.AddRange(configDepsContent.Deps);
        deps = RelaxDeps(deps);
        return new DepsData(force, deps);
    }

    public DepsData GetDepsFromConfig(string configName)
    {
        try
        {
            var configSection = GetConfigurationSection(configName);
            return GetDepsFromSection(configSection);
        }
        catch (BadYamlException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new BadYamlException(ModuleName, "deps", exception.Message);
        }
    }

    private static DepsData GetDepsFromSection(Dictionary<string, object> configSection)
    {
        if (!configSection.ContainsKey("deps"))
            return new DepsData(null, new List<Dep>());

        if (configSection["deps"] == null || configSection["deps"] is string)
            return new DepsData(null, new List<Dep>());

        var deps = new List<Dep>();
        string[] force = null;
        foreach (var depSection in (List<object>)configSection["deps"])
        {
            if (depSection is Dictionary<object, object> dict)
            {
                if (dict.Keys.Count == 1 && (string)dict.Keys.First() == "force")
                    force = ((string)dict.Values.First()).Split(',');
                else
                    deps.Add(GetDepFromDictFormat(dict));
            }
            else
                deps.Add(new Dep(depSection.ToString()));
        }

        return new DepsData(force, deps);
    }

    private static Dep GetDepFromDictFormat(Dictionary<object, object> dict)
    {
        string name = null, treeish = null, configuration = null;
        foreach (var kvp in dict)
        {
            if ((string)kvp.Key == "treeish")
                treeish = (string)kvp.Value;
            if ((string)kvp.Key == "configuration")
                configuration = (string)kvp.Value;
            if ((string)kvp.Value == "")
                name = (string)kvp.Key;
        }

        var dep = new Dep(name);
        if (configuration != null)
            dep.Configuration = configuration;
        if (treeish != null)
            dep.Treeish = treeish;
        return dep;
    }

    private List<Dep> RelaxDeps(List<Dep> deps)
    {
        var relaxedDeps = new List<Dep>();
        for (var i = 0; i < deps.Count; ++i)
        {
            if (deps[i].Name.StartsWith("-"))
            {
                if (i == deps.Count - 1 || !deps[i + 1].Name.Equals(deps[i].Name[1..]))
                    throw new BadYamlException(ModuleName, "deps", "dep " + deps[i].Name + " was deleted, but not added");
                RemoveDep(deps[i], relaxedDeps);
            }
            else
            {
                relaxedDeps.Add(deps[i]);
            }
        }

        foreach (var dep in relaxedDeps)
            if (relaxedDeps.Count(d => d.Name.Equals(dep.Name)) > 1)
            {
                consoleWriter.WriteError(
                    string.Format(
                        @"Module duplication found in 'module.yaml' for dep {0}. To depend on different variations of same dep, you must turn it off.
Example:
client:
  dep:
    - {0}/client
sdk:
  dep:
    - -{0}
    - {0}/full-build", dep.Name));
                throw new BadYamlException(ModuleName, "deps", "duplicate dep " + dep.Name);
            }

        return relaxedDeps;
    }

    private void RemoveDep(Dep dep, List<Dep> deps)
    {
        var i = 0;
        var deleted = false;
        while (i < deps.Count)
        {
            if (DepMatch(dep, deps[i]))
            {
                deleted = true;
                deps.Remove(deps[i]);
            }
            else
                i++;
        }

        if (!deleted)
            throw new BadYamlException(ModuleName, "deps", "fail to remove dep " + dep.Name);
    }

    private bool DepMatch(Dep expected, Dep actual)
    {
        if (!expected.Name[1..].Equals(actual.Name))
            return false;
        var treeishMatch = false;
        var configMatch = false;

        if (expected.Treeish is null or "*" || expected.Treeish.Equals(actual.Treeish))
            treeishMatch = true;
        if (expected.Configuration is null or "*" || expected.Configuration.Equals(actual.Configuration))
            configMatch = true;
        return treeishMatch && configMatch;
    }

    private DepsData GetDepsContent(string configuration)
    {
        var force = GetDepsFromConfig(configuration).Force;
        var configQueue = new List<string>();
        var deps = new List<Dep>();
        configQueue.Add(configuration);
        var idx = 0;
        while (idx < configQueue.Count)
        {
            var currentConfig = configQueue[idx];
            var currentDeps = GetDepsFromConfig(currentConfig).Deps;

            var depsValidator = depsValidatorFactory.Create(currentConfig);
            if (depsValidator.Validate(currentDeps, out var validateErrors) != DepsValidateResult.Valid)
            {
                foreach (var validateError in validateErrors)
                    consoleWriter.WriteWarning(validateError);

                var variable = Environment.GetEnvironmentVariable("CEMENT__STRICT_DEPS");
                if (bool.TryParse(variable, out var strict) && strict)
                    throw new BadYamlException(ModuleName, "deps", "The module dependencies are not valid");
            }

            currentDeps.AddRange(deps.Where(dep => !currentDeps.Contains(dep)));
            deps = currentDeps;
            if (!IsInherited(currentConfig))
            {
                idx++;
                continue;
            }

            var parentConfigurations = GetParentConfigurations(currentConfig);
            if (parentConfigurations == null || parentConfigurations.Count == 0)
            {
                idx++;
                continue;
            }

            configQueue.AddRange(parentConfigurations);
            idx++;
        }

        return new DepsData(force, deps);
    }
}
