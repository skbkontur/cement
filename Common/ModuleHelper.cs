using System.IO;
using System.Linq;
using Common.Exceptions;
using JetBrains.Annotations;

namespace Common;

[PublicAPI]
public sealed class ModuleHelper
{
    private readonly ConsoleWriter consoleWriter;
    private readonly IGitRepositoryFactory gitRepositoryFactory;
    private readonly IPackageUpdater packageUpdater;

    public ModuleHelper(ConsoleWriter consoleWriter, IGitRepositoryFactory gitRepositoryFactory, IPackageUpdater packageUpdater)
    {
        this.consoleWriter = consoleWriter;
        this.gitRepositoryFactory = gitRepositoryFactory;
        this.packageUpdater = packageUpdater;
    }

    public int AddModule(Package package, string moduleName, string pushUrl, string fetchUrl)
    {
        if (fetchUrl.StartsWith("https://git.skbkontur.ru/"))
            throw new CementException("HTTPS url not allowed for gitlab. You should use SSH url.");
        using (var tempDir = new TempDirectory())
        {
            var repo = gitRepositoryFactory.Create("modules_git", tempDir.Path);
            repo.Clone(package.Url);
            if (FindModule(repo, moduleName) != null)
            {
                consoleWriter.WriteError("Module " + moduleName + " already exists in " + package.Name);
                return -1;
            }

            WriteModuleDescription(moduleName, pushUrl, fetchUrl, repo);

            var message = "(!)cement comment: added module '" + moduleName + "'";
            repo.Commit("-am", message);
            repo.Push("master");
        }

        consoleWriter.WriteOk($"Successfully added {moduleName} to {package.Name} package.");

        packageUpdater.UpdatePackages();

        return 0;
    }

    public int ChangeModule(Package package, string moduleName, string pushUrl, string fetchUrl)
    {
        using (var tempDir = new TempDirectory())
        {
            var repo = gitRepositoryFactory.Create("modules_git", tempDir.Path);
            repo.Clone(package.Url);

            var toChange = FindModule(repo, moduleName);
            if (toChange == null)
            {
                consoleWriter.WriteError("Unable to find module " + moduleName + " in package " + package.Name);
                return -1;
            }

            if (toChange.Url == fetchUrl && toChange.Pushurl == pushUrl)
            {
                consoleWriter.WriteInfo("Your changes were already made");
                return 0;
            }

            ChangeModuleDescription(repo, toChange, new Module(moduleName, fetchUrl, pushUrl));

            var message = "(!)cement comment: changed module '" + moduleName + "'";
            repo.Commit("-am", message);
            repo.Push("master");
        }

        consoleWriter.WriteOk("Success changed " + moduleName + " in " + package.Name);
        return 0;
    }

    private static Module FindModule(GitRepository repo, string moduleName)
    {
        var content = File.ReadAllText(Path.Combine(repo.RepoPath, "modules"));
        var modules = ModuleIniParser.Parse(content);
        return modules.FirstOrDefault(m => m.Name == moduleName);
    }

    private static void WriteModuleDescription(string moduleName, string pushUrl, string fetchUrl, GitRepository repo)
    {
        var filePath = Path.Combine(repo.RepoPath, "modules");
        if (!File.Exists(filePath))
            File.Create(filePath).Close();
        File.AppendAllLines(
            filePath, new[]
            {
                "",
                "[module " + moduleName + "]",
                "url = " + fetchUrl,
                pushUrl != null ? "pushurl = " + pushUrl : ""
            });
    }

    private static void ChangeModuleDescription(GitRepository repo, Module old, Module changed)
    {
        var filePath = Path.Combine(repo.RepoPath, "modules");
        var lines = File.ReadAllLines(filePath);

        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i] != "[module " + old.Name + "]")
                continue;
            lines[i + 1] = "url = " + changed.Url;
            lines[i + 2] = changed.Pushurl == null ? "" : "pushurl = " + changed.Pushurl;
        }

        File.WriteAllLines(filePath, lines);
    }
}
