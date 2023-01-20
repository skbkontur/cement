using System;
using System.IO;
using System.Linq;
using System.Xml;
using Cement.Cli.Commands.Extensions;
using Cement.Cli.Common;
using Cement.Cli.Common.ArgumentsParsing;
using Cement.Cli.Common.Exceptions;
using Cement.Cli.Common.YamlParsers;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class RefAddCommand : Command<RefAddCommandOptions>
{
    private readonly ILogger<RefAddCommand> logger;
    private readonly ConsoleWriter consoleWriter;
    private readonly BuildDepsCommand buildDepsCommand;
    private readonly BuildCommand buildCommand;
    private readonly IGitRepositoryFactory gitRepositoryFactory;
    private readonly ICommandActivator commandActivator;
    private readonly IPackageUpdater packageUpdater;
    private readonly DepsPatcherProject depsPatcherProject;

    private bool hasReplaces;

    public RefAddCommand(ILogger<RefAddCommand> logger, ConsoleWriter consoleWriter, BuildDepsCommand buildDepsCommand,
                         BuildCommand buildCommand, DepsPatcherProject depsPatcherProject,
                         IGitRepositoryFactory gitRepositoryFactory, ICommandActivator commandActivator,
                         IPackageUpdater packageUpdater)
        : base(consoleWriter)
    {
        this.logger = logger;
        this.consoleWriter = consoleWriter;
        this.buildDepsCommand = buildDepsCommand;
        this.buildCommand = buildCommand;
        this.depsPatcherProject = depsPatcherProject;
        this.gitRepositoryFactory = gitRepositoryFactory;
        this.commandActivator = commandActivator;
        this.packageUpdater = packageUpdater;
    }

    public override CommandLocation Location { get; set; } = CommandLocation.InsideModuleDirectory;
    public override string Name => "add";
    public override string HelpMessage => @"";

    protected override RefAddCommandOptions ParseArgs(string[] args)
    {
        var parsedArgs = ArgumentParser.ParseRefAdd(args);

        var testReplaces = (bool)parsedArgs["testReplaces"];
        var dep = new Dep((string)parsedArgs["module"]);
        if (parsedArgs["configuration"] != null)
            dep.Configuration = (string)parsedArgs["configuration"];

        var project = (string)parsedArgs["project"];
        var force = (bool)parsedArgs["force"];
        if (!project.EndsWith(".csproj"))
            throw new BadArgumentException(project + " is not csproj file");

        return new RefAddCommandOptions(project, dep, testReplaces, force);
    }

    protected override int Execute(RefAddCommandOptions options)
    {
        var currentModuleDirectory = Helper.GetModuleDirectory(Directory.GetCurrentDirectory());
        var currentModule = Path.GetFileName(currentModuleDirectory);

        packageUpdater.UpdatePackages();
        var project = Yaml.GetProjectFileName(options.Project, currentModule);

        var moduleToInsert = Helper.TryFixModuleCase(options.Dep.Name);
        var dep = new Dep(moduleToInsert, options.Dep.Treeish, options.Dep.Configuration);
        var configuration = dep.Configuration;

        if (!Helper.HasModule(moduleToInsert))
        {
            consoleWriter.WriteError($"Can't find module '{moduleToInsert}'");
            return -1;
        }

        if (!Directory.Exists(Path.Combine(Helper.CurrentWorkspace, moduleToInsert)))
            GetAndBuild(dep);

        logger.LogDebug(
            $"{moduleToInsert + (configuration == null ? "" : Helper.ConfigurationDelimiter + configuration)} -> {project}");

        CheckBranch(dep);

        logger.LogInformation("Getting install data for " + moduleToInsert + Helper.ConfigurationDelimiter + configuration);
        var installData = InstallParser.Get(moduleToInsert, configuration);
        if (!installData.InstallFiles.Any())
        {
            consoleWriter.WriteWarning($"No install files found in '{moduleToInsert}'");
            return 0;
        }

        AddModuleToCsproj(dep, project, options.Force, options.TestReplaces, installData);
        if (options.TestReplaces)
            return hasReplaces ? -1 : 0;

        if (!File.Exists(Path.Combine(currentModuleDirectory, Helper.YamlSpecFile)))
            throw new CementException(
                "No module.yaml file. You should patch deps file manually or convert old spec to module.yaml (cm convert-spec)");
        depsPatcherProject.PatchDepsForProject(currentModuleDirectory, dep, project);
        return 0;
    }

    private void SafeAddRef(ProjectFile csproj, string refName, string hintPath)
    {
        try
        {
            csproj.AddRef(refName, hintPath);
        }
        catch (Exception e)
        {
            consoleWriter.WriteLine(e.ToString());
            logger.LogError(e, "Fail to add reference");
        }
    }

    private void GetAndBuild(Dep module)
    {
        using (new DirectoryJumper(Helper.CurrentWorkspace))
        {
            consoleWriter.WriteInfo("cm get " + module);

            var getCommand = commandActivator.Create<GetCommand>();
            if (getCommand.Run(new[] {"get", module.ToYamlString()}) != 0)
                throw new CementException("Failed get module " + module);

            consoleWriter.ResetProgress();
        }

        module.Configuration = module.Configuration ?? Yaml.ConfigurationParser(module.Name).GetDefaultConfigurationName();

        using (new DirectoryJumper(Path.Combine(Helper.CurrentWorkspace, module.Name)))
        {
            consoleWriter.WriteInfo("cm build-deps " + module);
            if (buildDepsCommand.Run(new[] {"build-deps", "-c", module.Configuration}) != 0)
                throw new CementException("Failed to build deps for " + module);
            consoleWriter.ResetProgress();
            consoleWriter.WriteInfo("cm build " + module);
            if (buildCommand.Run(new[] {"build", "-c", module.Configuration}) != 0)
                throw new CementException("Failed to build " + module);
            consoleWriter.ResetProgress();
        }

        consoleWriter.WriteLine();
    }

    private void CheckBranch(Dep dep)
    {
        if (string.IsNullOrEmpty(dep.Treeish))
            return;

        try
        {
            var repo = gitRepositoryFactory.Create(dep.Name, Helper.CurrentWorkspace);
            var current = repo.CurrentLocalTreeish().Value;
            if (current != dep.Treeish)
                consoleWriter.WriteWarning($"{dep.Name} on @{current} but adding @{dep.Treeish}");
        }
        catch (Exception e)
        {
            logger.LogError(e, $"FAILED-TO-CHECK-BRANCH {dep}");
        }
    }

    private void AddModuleToCsproj(Dep dep, string project, bool force, bool testReplaces, InstallData installData)
    {
        var projectPath = Path.GetFullPath(project);
        var csproj = new ProjectFile(projectPath);

        try
        {
            csproj.InstallNuGetPackages(installData.NuGetPackages);
        }
        catch (Exception e)
        {
            consoleWriter.WriteWarning($"Installation of NuGet packages failed: {e.InnerException?.Message ?? e.Message}");
            logger.LogError(e, "Installation of NuGet packages failed:");
        }

        foreach (var buildItem in installData.InstallFiles)
        {
            var buildItemPath = Platform.IsUnix() ? Helper.WindowsPathSlashesToUnix(buildItem) : buildItem;
            var refName = Path.GetFileNameWithoutExtension(buildItemPath);

            var hintPath = Helper.GetRelativePath(
                Path.Combine(Helper.CurrentWorkspace, buildItemPath),
                Directory.GetParent(projectPath).FullName);

            if (Platform.IsUnix())
            {
                hintPath = Helper.UnixPathSlashesToWindows(hintPath);
            }

            AddRef(project, force, testReplaces, csproj, refName, hintPath);
            CheckExistBuildFile(dep, Path.Combine(Helper.CurrentWorkspace, buildItemPath));
        }

        if (!testReplaces)
            csproj.Save();
    }

    private void CheckExistBuildFile(Dep dep, string file)
    {
        if (File.Exists(file))
            return;
        consoleWriter.WriteWarning($"File {file} does not exist. Probably you need to build {dep.Name}.");
    }

    private void AddRef(string project, bool force, bool testReplaces, ProjectFile csproj, string refName, string hintPath)
    {
        if (testReplaces)
        {
            TestReplaces(csproj, refName);
            return;
        }

        XmlNode refXml;
        if (csproj.ContainsRef(refName, out refXml))
        {
            if (UserChoseReplace(project, force, csproj, refXml, refName, hintPath))
            {
                csproj.ReplaceRef(refName, hintPath);
                logger.LogDebug($"'{refName}' ref replaced");
                consoleWriter.WriteOk("Successfully replaced " + refName);
            }
        }
        else
        {
            SafeAddRef(csproj, refName, hintPath);
            logger.LogDebug($"'{refName}' ref added");
            consoleWriter.WriteOk("Successfully installed " + refName);
        }
    }

    private void TestReplaces(ProjectFile csproj, string refName)
    {
        XmlNode refXml;
        if (csproj.ContainsRef(refName, out refXml))
            hasReplaces = true;
    }

    private bool UserChoseReplace(string project, bool force, ProjectFile csproj, XmlNode refXml, string refName, string refPath)
    {
        if (force)
            return true;

        var elementToInsert = csproj.CreateReference(refName, refPath);
        var oldRef = refXml.OuterXml;
        var newRef = elementToInsert.OuterXml;

        if (oldRef.Equals(newRef))
        {
            consoleWriter.WriteSkip("Already has same " + refName);
            return false;
        }

        consoleWriter.WriteWarning(
            $"'{project}' already contains ref '{refName}'.\n\n<<<<\n{oldRef}\n\n>>>>\n{newRef}\nDo you want to replace (y/N)?");
        var answer = Console.ReadLine();
        return answer != null && answer.Trim().ToLowerInvariant() == "y";
    }
}
