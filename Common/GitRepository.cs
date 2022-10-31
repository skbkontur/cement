using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Common.Exceptions;
using Common.Logging;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Common;

[PublicAPI]
public sealed class GitRepository
{
    private readonly BuildHelper buildHelper;
    private readonly ILogger logger;
    private readonly ConsoleWriter consoleWriter;
    private readonly ShellRunner shellRunner;

    private static readonly char Quote = Platform.IsUnix() ? '\'' : '"';

    public GitRepository(ILogger<GitRepository> logger, ConsoleWriter consoleWriter, BuildHelper buildHelper,
                         ShellRunner shellRunner, string repoPath, string moduleName, string workspace)
    {
        this.logger = logger;
        this.consoleWriter = consoleWriter;
        this.buildHelper = buildHelper;
        this.shellRunner = shellRunner;

        ModuleName = moduleName;
        Workspace = workspace;
        RepoPath = repoPath;

        IsGitRepo = Directory.Exists(Path.Combine(workspace, moduleName, ".git"));
    }

    public static bool HasRemoteBranch(string url, string branchName)
    {
        if (string.IsNullOrEmpty(branchName))
            return false;

        var shellRunnerLogger = LogManager.GetLogger<ShellRunner>();
        var shellRunner = new ShellRunner(shellRunnerLogger);

        // todo(dstarasov): не проверяется exitCode
        var (_, output, _) = shellRunner.Run($"git ls-remote {url} {branchName}");

        return output.Contains("refs/heads");
    }

    public string RepoPath { get; }
    public string Workspace { get; }
    public string ModuleName { get; }
    public bool IsGitRepo { get; private set; }

    public void Clone(string url, string treeish = null, int? depth = null)
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Cloning treeish {treeish ?? "master"} into {RepoPath}");
        var treeishSuffix = "-b " + (treeish ?? "master");
        var depthSuffix = depth.HasValue ? $" --depth {depth.Value} --no-single-branch" : "";
        var cmd = $"git clone --recursive {url} {treeishSuffix}{depthSuffix} {Quote}{RepoPath}{Quote} 2>&1";

        var (exitCode, output, _) = shellRunner.Run(cmd, TimeSpan.FromMinutes(60), RetryStrategy.IfTimeoutOrFailed);
        if (exitCode != 0)
        {
            throw new GitCloneException($"Failed to clone {url}:{treeish}. Error message:{output}");
        }

        if (!Directory.Exists(Path.Combine(RepoPath, ".git")))
        {
            throw new GitCloneException($"Failed to clone {url}:{treeish}. Probably you don't have access to remote repository.");
        }

        IsGitRepo = true;
        RemoteBranches = GetRemoteBranches();
    }

    public void Init()
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Init in {RepoPath}");
        var cmd = $"git init {Quote}{RepoPath}{Quote}";

        var (exitCode, _, errors) = shellRunner.Run(cmd);
        if (exitCode != 0)
        {
            throw new GitInitException("Failed to init. Error message:\n" + errors);
        }

        if (!Directory.Exists(Path.Combine(RepoPath, ".git")))
        {
            throw new GitInitException("Failed to init repository. Probably you don't have access to remote repository.");
        }

        IsGitRepo = true;
    }

    public CurrentTreeish CurrentLocalTreeish()
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Getting current treeish");

        var (exitCode, branchName, _) = shellRunner.RunInDirectory(RepoPath, "git rev-parse --abbrev-ref HEAD");

        branchName = branchName.Trim();
        if (branchName != "HEAD")
            return new CurrentTreeish(TreeishType.Branch, branchName);

        if (exitCode != 0)
        {
            return new CurrentTreeish(TreeishType.Branch, "master");
        }

        // todo(dstarasov): не проверяется exitCode
        var (_, tags, _) = shellRunner.RunInDirectory(RepoPath, "git describe --tags --exact-match");
        tags = tags.Trim();

        if (tags.Length > 0)
        {
            return new CurrentTreeish(TreeishType.Tag, tags);
        }

        // todo(dstarasov): не проверяется exitCode
        var (_, commitHash, _) = shellRunner.RunInDirectory(RepoPath, "git rev-parse HEAD");
        commitHash = commitHash.Trim();

        return new CurrentTreeish(TreeishType.CommitHash, commitHash);
    }

    public string SafeGetCurrentLocalCommitHash(string treeish = null)
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30} Safe local commit hash at branch '{treeish ?? "HEAD"}'");
        var (exitCode, output, _) = shellRunner.RunInDirectory(RepoPath, "git rev-parse " + (treeish ?? "HEAD"));

        if (exitCode != 0)
        {
            logger.LogWarning($"Failed to get local commit hash in {ModuleName}");
            return "";
        }

        return output.Trim();
    }

    public string CurrentLocalCommitHash(string treeish = null)
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Local commit hash at branch '{treeish ?? "HEAD"}'");
        var (exitCode, output, errors) = shellRunner.RunInDirectory(RepoPath, "git rev-parse " + (treeish ?? "HEAD"));

        if (exitCode != 0)
        {
            throw new GitTreeishException(
                $"Failed to get commit hash for treeish {treeish ?? "master"} in {RepoPath}. Error message:\n{errors}");
        }

        return output.Trim();
    }

    public void Checkout(string treeish, bool track = false)
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Checkout {treeish}");

        var command = HasLocalBranch(treeish) || !track
            ? "git checkout " + treeish
            : $"git checkout -b {treeish} --track origin/{treeish}";

        var (exitCode, output, _) = shellRunner.RunInDirectory(RepoPath, command, TimeSpan.FromMinutes(60));
        if (exitCode == 0)
        {
            return;
        }

        logger.LogInformation($"pull {ModuleName}");
        shellRunner.RunInDirectory(RepoPath, "git pull", TimeSpan.FromMinutes(60));
        logger.LogDebug($"pull result {ModuleName} {output}");

        var (exitCode2, _, errors) = shellRunner.RunInDirectory(RepoPath, command, TimeSpan.FromMinutes(60));
        if (exitCode2 != 0)
        {
            throw new GitCheckoutException($"Failed to checkout to {treeish} from {ModuleName}. {errors}");
        }
    }

    public void SubmoduleUpdate()
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Submodule init");

        if (!File.Exists(Path.Combine(RepoPath, ".gitmodules")))
        {
            logger.LogInformation($"{"[" + ModuleName + "]",-30} No submodules found");
            return;
        }

        var command = "git submodule update --init --recursive";

        var (exitCode, _, errors) = shellRunner.RunInDirectory(RepoPath, command, TimeSpan.FromMinutes(60));
        if (exitCode != 0)
        {
            throw new GitCheckoutException($"Failed to checkout to submodule update from {ModuleName}. {errors}");
        }
    }

    public void Fetch(string branch, int? depth = null)
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Fetching {branch}");

        var depthSuffix = depth.HasValue ? $" --depth {depth.Value}" : "";
        var command = "git fetch origin " + branch + depthSuffix;

        var (exitCode, _, errors) = shellRunner.RunInDirectory(RepoPath, command, TimeSpan.FromMinutes(60), RetryStrategy.IfTimeoutOrFailed);
        if (exitCode != 0)
        {
            throw new GitPullException($"Failed to fetch {RepoPath}:{branch}. Error message:\n{errors}");
        }
    }

    public void Pull(string treeish, int? depth = null)
    {
        Fetch("", depth);
        Merge("origin/" + treeish);
    }

    public bool HasRemoteBranch(string branch)
    {
        if (RemoteBranches == null)
            RemoteBranches = GetRemoteBranches();
        return RemoteBranches.Any(b => b.Name == branch);
    }

    public string RemoteCommitHashAtBranch(string branch)
    {
        if (RemoteBranches == null)
            RemoteBranches = GetRemoteBranches();
        return RemoteBranches.First(b => b.Name == branch).CommitHash;
    }

    public string RemoteCommitHashAtTreeish(string treeish)
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Getting treeish remote hash");
        var (exitCode, output, _) = shellRunner.RunInDirectory(RepoPath, "git ls-remote origin " + treeish);

        if (exitCode != 0)
            throw new GitTreeishException("Fail get remote commit hash at " + ModuleName + "@" + treeish);

        return output.Split('\t')[0];
    }

    // ReSharper disable once UnusedMember.Global
    public void RewriteFileFromRemote(string branch, string shortPath, string destination)
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Rewrite file from remote {branch}:{shortPath}");

        shortPath = shortPath.Replace(Path.DirectorySeparatorChar.ToString(), "/");
        var (exitCode, _, errors) = shellRunner.RunInDirectory(RepoPath, $"git show origin/{branch}:{shortPath} > {destination}");
        if (exitCode != 0)
        {
            throw new GitRemoteException(
                $"Failed to rewrite file from remote {RepoPath}:{branch}:{shortPath}. Error message:\n{errors}");
        }
    }

    public string ShowLocalChanges()
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Show local changes");

        var (exitCode, output, errors) = shellRunner.RunInDirectory(RepoPath, "git status -s");
        if (exitCode != 0)
        {
            throw new GitLocalChangesException($"Failed to get local changes in {RepoPath}. Error message:\n{errors}");
        }

        return output;
    }

    public bool HasLocalChanges()
    {
        return ShowLocalChanges().Split('\n').Any(line => line.Trim().Length > 0);
    }

    public IList<string> LocalBranches()
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Get local branches");

        var (exitCode, output, errors) = shellRunner.RunInDirectory(RepoPath, "git branch");
        if (exitCode != 0)
        {
            throw new GitBranchException($"Failed to get local branches in {RepoPath}. Error message:\n{errors}");
        }

        var lines = output.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
        return lines.Select(l => l.Replace("*", "").Trim()).ToArray();
    }

    public void AddOrigin(string url)
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Add origin");

        var (exitCode, _, errors) = shellRunner.RunInDirectory(RepoPath, "git remote add origin " + url);
        if (exitCode != 0)
        {
            throw new GitCheckoutException($"Failed to add origin to {ModuleName}. Error message:\n{errors}");
        }
    }

    public void SetPushUrl(string url)
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Set push url");

        var (exitCode, _, errors) = shellRunner.RunInDirectory(RepoPath, "git remote set-url origin --push " + url);
        if (exitCode != 0)
        {
            throw new GitCheckoutException($"Failed to set push url origin to {ModuleName}. Error message:\n{errors}");
        }
    }

    public void DeleteUntrackedFiles()
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Deliting untaracked files");

        var (exitCode, _, errors) = shellRunner.RunInDirectory(RepoPath, "git clean -f -q");
        if (exitCode != 0)
        {
            throw new GitLocalChangesException($"Failed to clean local changes in {RepoPath}. Error message:\n{errors}");
        }
    }

    public void Clean()
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Clean and reset hard");
        var gitIgnore = Path.Combine(RepoPath, ".gitignore");
        if (File.Exists(gitIgnore))
            File.Delete(gitIgnore);
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Remove from built cache");
        buildHelper.RemoveModuleFromBuiltInfo(ModuleName);

        var (exitCode, _, errors) = shellRunner.RunInDirectory(RepoPath, "git clean -f -d -q");
        if (exitCode != 0)
        {
            throw new GitLocalChangesException($"Failed to clean local changes in {RepoPath}. Error message:\n{errors}");
        }

        var (exitCode2, _, errors2) = shellRunner.RunInDirectory(RepoPath, "git reset --hard");
        if (exitCode2 != 0)
        {
            throw new GitLocalChangesException($"Failed to reset local changes in {RepoPath}. Error message:\n{errors2}");
        }
    }

    public IList<Branch> GetRemoteBranches()
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Get remote branches");
        var sw = Stopwatch.StartNew();

        var (exitCode, output, errors) = shellRunner
            .RunInDirectory(RepoPath, "git ls-remote --heads", TimeoutHelper.GetStartTimeout(), RetryStrategy.IfTimeoutOrFailed);

        sw.Stop();
        if (sw.Elapsed > TimeSpan.FromSeconds(10))
            logger.LogDebug("{0, -30}Elapsed git ls-remote --heads: [{1}]", "[" + ModuleName + "]", sw.Elapsed);

        if (exitCode != 0)
        {
            throw new GitBranchException(
                $"Failed to get remote branches in {RepoPath}. Error message:\n{errors}");
        }

        var branches = output.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
        return branches.Select(Branch.Parse).Where(b => b.Name != null).ToList();
    }

    public bool HasLocalBranch(string branch)
    {
        return LocalBranches().Contains(branch);
    }

    public bool IsKnownRemoteBranch(string branch)
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Is known remote branch '{branch}'");

        var (exitCode, output, errors) = shellRunner.RunInDirectory(RepoPath, "git branch -r");
        if (exitCode != 0)
        {
            throw new GitBranchException(
                $"Failed to get list of known remote branches in {RepoPath}. Error message:\n{errors}");
        }

        var knownRemoteBranches = output
            .Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split('/').Last());
        return knownRemoteBranches.Contains(branch);
    }

    public void ResetHard(string treeish = null)
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Reset hard {treeish}");
        buildHelper.RemoveModuleFromBuiltInfo(ModuleName);
        shellRunner.RunInDirectory(RepoPath, "git reset --hard " + (treeish == null ? "" : "origin/" + treeish));
    }

    public bool FastForwardPullAllowed(string treeish)
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Fast forward pull allowed for {treeish}");
        Fetch(treeish);

        var (exitCode, output, errors) = shellRunner.RunInDirectory(RepoPath, $"git merge-base {treeish} {"origin/" + treeish}");
        if (exitCode != 0)
        {
            throw new GitTreeishException($"Failed to get merge-base in {RepoPath}. Error message:\n{errors}");
        }

        var mergeBase = output.Trim();
        return mergeBase.Equals(CurrentLocalCommitHash()) || mergeBase.Equals(RemoteCommitHashAtBranch(treeish));
    }

    public bool BranchWasMerged(string treeish)
    {
        var (exitCode, output, errors) = shellRunner.RunInDirectory(RepoPath, "git branch -a --merged");
        if (exitCode != 0)
        {
            throw new GitBranchException($"Failed to get list of merged branches in {RepoPath}. Error message:\n{errors}");
        }

        var lines = output.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
        return lines
            .Select(l => l.Replace("*", "").Trim())
            .Any(l => l.EndsWith("/" + treeish));
    }

    public void RemoteOriginUrls(out string fetchUrl, out string pushUrl)
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Remote origin URL");

        var (exitCode, output, errors) = shellRunner.RunInDirectory(RepoPath, "git remote -v");
        if (exitCode != 0)
        {
            throw new GitRemoteException($"Failed to get remote url in {RepoPath}. Error message:\n{errors}\n{output}");
        }

        var lines = output
            .Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim()).ToList();

        var linesWithFetch = lines.Where(l => l.StartsWith("origin") && l.EndsWith("(fetch)")).ToList();
        fetchUrl = linesWithFetch.Any() ? linesWithFetch.First().Split().ElementAt(1) : "";
        var linesWithPush = lines.Where(l => l.StartsWith("origin") && l.EndsWith("(push)")).ToList();
        pushUrl = linesWithPush.Any() ? linesWithPush.First().Split().ElementAt(1) : "";
    }

    public void TryUpdateUrl(Module module)
    {
        if (module == null)
            return;

        var targetPushUrl = module.Pushurl ?? module.Url;

        RemoteOriginUrls(out var fetchUrl, out var pushUrl);

        if (fetchUrl.Equals(module.Url) && pushUrl.Equals(targetPushUrl))
            return;

        logger.LogInformation($"{"[" + ModuleName + "]",-30}Updating URL");
        consoleWriter.WriteInfo($"Update url for {RepoPath}: {fetchUrl} => {module.Url}");
        RemoveOrigin();
        AddOrigin(module.Url);

        if (module.Pushurl != null)
        {
            logger.LogInformation($"{"[" + ModuleName + "]",-30}Updating push URL");
            consoleWriter.WriteInfo($"Update push url for {RepoPath}: {pushUrl} => {module.Pushurl}");
            SetPushUrl(module.Pushurl);
        }
    }

    public List<string> GetFilesForCommit()
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Get files for commit");

        var (exitCode, output, errors) = shellRunner.RunInDirectory(RepoPath, "git diff --cached --name-only");
        if (exitCode != 0)
        {
            throw new GitCommitException($"Failed to get commit files in {RepoPath}. Error message:\n{errors}");
        }

        var lines = output.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
        return lines.ToList();
    }

    public string GetCommitInfo()
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Get commit info");
        var (exitCode, output, _) = shellRunner
            .RunInDirectory(RepoPath, $"git log HEAD -n 1 --date=relative --format={Quote}%ad\t{Quote}%an{Quote}\t<%ae>{Quote}");

        if (exitCode != 0)
        {
            return "Failed to get commit info";
        }

        var tokens = output.Trim()
            .Split('\t')
            .ToArray();
        return $"{tokens[0],-25}" + tokens[1] + " " + tokens[2];
    }

    public void Commit(params string[] args)
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}git commit {args.Aggregate("", (x, y) => x + $" {Quote}" + y + Quote)}");

        var (exitCode, output, errors) = shellRunner
            .RunInDirectory(RepoPath, "git commit" + args.Aggregate("", (x, y) => x + $" {Quote}" + y + Quote));

        if (exitCode != 0)
        {
            throw new GitCommitException($"Failed to commit in {RepoPath}. Output: {output}; Errors: {errors}");
        }
    }

    public string ShowUnpushedCommits()
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Show unpushed commits");

        var (exitCode, output, _) = shellRunner
            .RunInDirectory(RepoPath, "git log --branches --not --remotes=origin --simplify-by-decoration --decorate --oneline");

        if (exitCode != 0)
        {
            throw new GitLocalChangesException($"Failed to get unpushed changes in {RepoPath}.");
        }

        return output;
    }

    public void Push(string branch)
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Push origin {branch}");

        var (exitCode, output, errors) = shellRunner
            .RunInDirectory(RepoPath, "git push origin " + branch, TimeSpan.FromMinutes(60));

        if (exitCode != 0)
        {
            throw new GitPushException($"Failed to push {RepoPath}:{branch}. Error:\n{output + errors}");
        }
    }

    private IList<Branch> RemoteBranches { get; set; }

    private void Merge(string treeish)
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Merge --ff-only '{treeish}'");

        var (exitCode, _, errors) = shellRunner
            .RunInDirectory(RepoPath, "git merge --ff-only " + treeish, TimeSpan.FromMinutes(60));

        if (exitCode != 0)
        {
            throw new GitPullException(
                $"Failed to fast-forward pull in {RepoPath} for branch {treeish}. Error message:\n{errors}");
        }
    }

    private void RemoveOrigin()
    {
        logger.LogInformation($"{"[" + ModuleName + "]",-30}Remove origin");

        var (exitCode, _, errors) = shellRunner.RunInDirectory(RepoPath, "git remote rm origin ");
        if (exitCode != 0)
        {
            throw new GitCheckoutException($"Failed to remove in {ModuleName}. Error message:\n{errors}");
        }
    }
}
