using System.Collections.Generic;
using System.IO;
using Common.DepsValidators;
using Common.YamlParsers;

namespace Common;

public sealed class DepsParser
{
    private readonly ConsoleWriter consoleWriter;
    private readonly IDepsValidatorFactory depsValidatorFactory;
    private readonly string modulePath;

    public DepsParser(ConsoleWriter consoleWriter, IDepsValidatorFactory depsValidatorFactory, string modulePath)
    {
        this.consoleWriter = consoleWriter;
        this.depsValidatorFactory = depsValidatorFactory;
        this.modulePath = modulePath;
    }

    public DepsData Get(string config = null)
    {
        return File.Exists(Path.Combine(modulePath, Helper.YamlSpecFile))
            ? new DepsYamlParser(consoleWriter, depsValidatorFactory, new FileInfo(modulePath)).Get(config)
            : new DepsData(null, new List<Dep>());
    }
}
