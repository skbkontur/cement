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
        var shellRunnerLogger = LogManager.GetLogger<ShellRunner>();
        var shellRunner = new ShellRunner(shellRunnerLogger);

        var repoPath = Path.Combine(workspace, moduleName);

        var gitRepositoryLogger = LogManager.GetLogger<GitRepository>();
        return new GitRepository(gitRepositoryLogger, consoleWriter, buildHelper, shellRunner, repoPath, moduleName, workspace);
    }

    public GitRepository Create(string repoPath)
    {
        var shellRunnerLogger = LogManager.GetLogger<ShellRunner>();
        var shellRunner = new ShellRunner(shellRunnerLogger);

        var moduleName = Path.GetFileName(repoPath);
        var workspace = Directory.GetParent(repoPath)!.FullName;

        var gitRepositoryLogger = LogManager.GetLogger<GitRepository>();
        return new GitRepository(gitRepositoryLogger, consoleWriter, buildHelper, shellRunner, repoPath, moduleName, workspace);
    }
}
