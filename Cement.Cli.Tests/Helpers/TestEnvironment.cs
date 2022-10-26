using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Common;
using Common.DepsValidators;
using Common.Logging;
using Common.YamlParsers;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cement.Cli.Tests.Helpers;

public class TestEnvironment : IDisposable
{
    public readonly TempDirectory WorkingDirectory;
    public readonly string PackageFile;
    public readonly string RemoteWorkspace;

    private readonly ShellRunner runner;
    private readonly CycleDetector cycleDetector;
    private readonly ConsoleWriter consoleWriter;
    private readonly IDepsValidatorFactory depsValidatorFactory;
    private readonly IGitRepositoryFactory gitRepositoryFactory;
    private readonly HooksHelper hooksHelper;

    public TestEnvironment()
    {
        WorkingDirectory = new TempDirectory();

        consoleWriter = ConsoleWriter.Shared;
        depsValidatorFactory = new DepsValidatorFactory();

        var buildHelper = BuildHelper.Shared;
        hooksHelper = new HooksHelper(NullLogger<HooksHelper>.Instance, consoleWriter);

        gitRepositoryFactory = new GitRepositoryFactory(consoleWriter, buildHelper);
        cycleDetector = new CycleDetector(consoleWriter, depsValidatorFactory);

        runner = new ShellRunner(NullLogger<ShellRunner>.Instance);

        Directory.CreateDirectory(Path.Combine(WorkingDirectory.Path, ".cement"));
        RemoteWorkspace = Path.Combine(WorkingDirectory.Path, "remote");
        Directory.CreateDirectory(Path.Combine(RemoteWorkspace, ".cement"));
        PackageFile = Path.Combine(WorkingDirectory.Path, "package.cmpkg");
        Helper.SetWorkspace(WorkingDirectory.Path);
    }

    public void CreateRepo(string moduleName, Dictionary<string, DepsData> depsByConfig = null, IList<string> branches = null, DepsFormatStyle depsStyle = DepsFormatStyle.Yaml, string pushUrl = null)
    {
        var modulePath = Path.Combine(RemoteWorkspace, moduleName);
        using (new DirectoryJumper(modulePath))
        {
            CreateRepoAndCommitReadme();
            CreateDepsAndCommitThem(modulePath, depsByConfig, depsStyle);
            CreateBranches(branches);
        }

        AppendModule(moduleName, modulePath, pushUrl);
    }

    public void Get(string module, string treeish = null, LocalChangesPolicy localChangesPolicy = LocalChangesPolicy.FailOnLocalChanges)
    {
        var getter = new ModuleGetter(
            consoleWriter,
            cycleDetector,
            depsValidatorFactory,
            gitRepositoryFactory,
            hooksHelper,
            GetModules().ToList(),
            new Dep(module, treeish),
            localChangesPolicy,
            null);

        getter.GetModule();
        getter.GetDeps();
    }

    public Module[] GetModules()
    {
        return ModuleIniParser.Parse(File.ReadAllText(PackageFile));
    }

    public void CreateDepsAndCommitThem(string path, Dictionary<string, DepsData> depsByConfig, DepsFormatStyle depsStyle = DepsFormatStyle.Yaml)
    {
        if (depsStyle == DepsFormatStyle.Yaml)
            CreateDepsYamlStyle(path, depsByConfig);
    }

    public void Dispose()
    {
        Helper.SetWorkspace(null);
        LogManager.DisposeLoggers();
        WorkingDirectory.Dispose();
    }

    public void Checkout(string moduleName, string branch)
    {
        using (new DirectoryJumper(Path.Combine(RemoteWorkspace, moduleName)))
        {
            runner.Run("git checkout " + branch);
        }
    }

    public void AddBranch(string moduleName, string branch)
    {
        using (new DirectoryJumper(Path.Combine(RemoteWorkspace, moduleName)))
        {
            runner.Run("git branch " + branch);
        }
    }

    public void ChangeUrl(string repoPath, string destPath)
    {
        var content = File.ReadAllText(PackageFile);
        content = content.Replace(Path.Combine(RemoteWorkspace, repoPath), Path.Combine(RemoteWorkspace, destPath));
        File.Delete(PackageFile);
        File.WriteAllText(PackageFile, content);
    }

    public void CommitIntoLocal(string moduleName, string newfile, string content)
    {
        Commit(Path.Combine(WorkingDirectory.Path, moduleName), newfile, content);
    }

    public void CommitIntoRemote(string moduleName, string newfile, string content)
    {
        Commit(Path.Combine(RemoteWorkspace, moduleName), newfile, content);
    }

    public void MakeLocalChanges(string moduleName, string file, string content)
    {
        File.WriteAllText(Path.Combine(WorkingDirectory.Path, moduleName, file), content);
    }

    private void AppendModule(string moduleName, string modulePath, string pushUrl)
    {
        var sb = new StringBuilder()
            .AppendLine()
            .AppendLine($"[module {moduleName}]")
            .AppendLine($"url={modulePath}");

        if (pushUrl != null)
            sb.AppendLine($"pushurl={pushUrl}");

        File.AppendAllText(Path.Combine(RemoteWorkspace, PackageFile), sb.ToString());
    }

    private void CreateBranches(IList<string> branches)
    {
        if (branches == null)
            return;
        foreach (var branch in branches)
        {
            runner.Run("git branch " + branch);
        }
    }

    private void CreateDepsYamlStyle(string path, Dictionary<string, DepsData> depsByConfig)
    {
        if (depsByConfig == null)
            return;

        var content = "default:";

        if (depsByConfig.Keys.Count == 0)
        {
            content = @"default:
full-build:
  build:
    target: None
    configuration: None";
        }

        foreach (var config in depsByConfig.Keys.OrderBy(x => x))
        {
            content += depsByConfig[config].Deps.Aggregate(
                $@"
{config}:
  build:
    target: None
    configuration: None
  deps:{
      (depsByConfig[config]
          .Force != null
          ? "\r\n    - force: " + string.Join(",", depsByConfig[config].Force)
          : "")
  }
", (current, dep) => current +
                     $"    - {dep.Name}@{dep.Treeish ?? ""}/{dep.Configuration ?? ""}\r\n");
        }

        File.WriteAllText(Path.Combine(path, "module.yaml"), content);
        runner.Run("git add module.yaml");
        runner.Run("git commit -am \"added deps\"");
    }

    private void CreateRepoAndCommitReadme()
    {
        runner.Run("git init");
        File.WriteAllText("README", "README");
        runner.Run("git add README");
        runner.Run("git commit -am \"initial commit\"");
    }

    private void Commit(string repoPath, string fileName, string content)
    {
        File.WriteAllText(Path.Combine(repoPath, fileName), content);
        using (new DirectoryJumper(repoPath))
        {
            runner.Run("git add " + fileName);
            runner.Run("git commit -am \"some commit\"");
        }
    }
}
