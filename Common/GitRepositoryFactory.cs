using System.IO;
using Common.Logging;
using JetBrains.Annotations;

namespace Common;

[PublicAPI]
public sealed class GitRepositoryFactory : IGitRepositoryFactory
{
    private readonly ConsoleWriter consoleWriter;
    private readonly BuildHelper buildHelper;

    public GitRepositoryFactory(ConsoleWriter consoleWriter, BuildHelper buildHelper)
    {
        this.consoleWriter = consoleWriter;
        this.buildHelper = buildHelper;
    }

    public GitRepository Create(string moduleName, string workspace)
    {
        var logger = LogManager.GetLogger<GitRepository>();
        var shellRunner = new ShellRunner(logger);

        var repoPath = Path.Combine(workspace, moduleName);

        return new GitRepository(logger, consoleWriter, buildHelper, shellRunner, repoPath, moduleName, workspace);
    }

    public GitRepository Create(string repoPath)
    {
        var logger = LogManager.GetLogger<GitRepository>();
        var shellRunner = new ShellRunner(logger);

        var moduleName = Path.GetFileName(repoPath);
        var workspace = Directory.GetParent(repoPath)!.FullName;

        return new GitRepository(logger, consoleWriter, buildHelper, shellRunner, repoPath, moduleName, workspace);
    }
}
