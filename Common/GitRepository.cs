using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using log4net;

namespace Common
{
    public enum TreeishType
    {
        Branch,
        Tag,
        CommitHash
    }

    [PublicAPI]
    public sealed class GitRepository
    {
        private readonly ShellRunner runner;
        private readonly ILog log;

        public GitRepository(string moduleName, string workspace, ILog log)
        {
            ModuleName = moduleName;
            Workspace = workspace;
            RepoPath = Path.Combine(workspace, moduleName);

            this.log = log;
            runner = new ShellRunner(log);
            IsGitRepo = Directory.Exists(Path.Combine(workspace, moduleName, ".git"));
        }

        public GitRepository(string fullPath, ILog log)
            : this(Path.GetFileName(fullPath), Directory.GetParent(fullPath).FullName, log)
        {
        }

        public static bool HasRemoteBranch(string url, string branchName)
        {
            if (string.IsNullOrEmpty(branchName))
                return false;
            var runner = new ShellRunner();
            runner.Run($"git ls-remote {url} {branchName}");
            return runner.Output.Contains("refs/heads");
        }

        public string RepoPath { get; }
        public string Workspace { get; }
        public string ModuleName { get; }
        public bool IsGitRepo { get; private set; }

        public GitSubmodule Submodule => new GitSubmodule(log, this, runner);

        public void Clone(string url, string branch = null, int depth = -1, bool recursive = true)
        {
            log.Info($"{"[" + ModuleName + "]",-30}Cloning branch {branch} into {RepoPath}");

            var commandBuilder = new StringBuilder("git clone");

            if (recursive)
                commandBuilder.Append(" --recursive");

            if (!string.IsNullOrWhiteSpace(branch))
                commandBuilder.AppendFormat(" --branch {0}", branch);

            if (depth > 0)
                commandBuilder.AppendFormat(" --depth {0}", depth);

            commandBuilder.Append(" --");
            commandBuilder.AppendFormat(" {0}", url);
            commandBuilder.AppendFormat(" \"{0}\"", RepoPath);
            commandBuilder.Append(" 2>&1");

            var command = commandBuilder.ToString();

            log.Debug($"{"[" + ModuleName + "]",-30}command: '{command}'");

            var exitCode = runner.Run(command, TimeSpan.FromMinutes(60), RetryStrategy.IfTimeoutOrFailed);
            if (exitCode != 0)
            {
                throw new GitCloneException($"Failed to clone {url}:{branch}. Error message:{runner.Output}");
            }

            if (!Directory.Exists(Path.Combine(RepoPath, ".git")))
            {
                throw new GitCloneException($"Failed to clone {url}:{branch}. Probably you don't have access to remote repository.");
            }

            IsGitRepo = true;
            RemoteBranches = GetRemoteBranches();
        }

        public void Init()
        {
            log.Info($"{"[" + ModuleName + "]",-30}Init in {RepoPath}");
            var cmd = $"git init \"{RepoPath}\"";
            var exitCode = runner.Run(cmd);
            if (exitCode != 0)
            {
                throw new GitInitException("Failed to init. Error message:\n" + runner.Errors);
            }

            if (!Directory.Exists(Path.Combine(RepoPath, ".git")))
            {
                throw new GitInitException("Failed to init repository. Probably you don't have access to remote repository.");
            }

            IsGitRepo = true;
        }

        public CurrentTreeish CurrentLocalTreeish()
        {
            log.Info($"{"[" + ModuleName + "]",-30}Getting current treeish");
            var exitCode = runner.RunInDirectory(RepoPath, "git rev-parse --abbrev-ref HEAD");

            var output = runner.Output.Trim();
            if (output != "HEAD")
                return new CurrentTreeish(TreeishType.Branch, output);

            if (exitCode != 0)
            {
                return new CurrentTreeish(TreeishType.Branch, "master");
            }

            runner.RunInDirectory(RepoPath, "git describe --tags --exact-match");
            var tags = runner.Output.Trim();
            if (tags.Length > 0)
            {
                return new CurrentTreeish(TreeishType.Tag, tags);
            }

            runner.RunInDirectory(RepoPath, "git rev-parse HEAD");
            return new CurrentTreeish(TreeishType.CommitHash, runner.Output.Trim());
        }

        public string SafeGetCurrentLocalCommitHash(string treeish = null)
        {
            log.Info($"{"[" + ModuleName + "]",-30} Safe local commit hash at branch '{treeish ?? "HEAD"}'");
            var exitCode = runner.RunInDirectory(RepoPath, "git rev-parse " + (treeish ?? "HEAD"));

            if (exitCode != 0)
            {
                log.Warn($"Failed to get local commit hash in {ModuleName}");
                return "";
            }

            return runner.Output.Trim();
        }

        public string CurrentLocalCommitHash(string treeish = null)
        {
            log.Info($"{"[" + ModuleName + "]",-30}Local commit hash at branch '{treeish ?? "HEAD"}'");
            var exitCode = runner.RunInDirectory(RepoPath, "git rev-parse " + (treeish ?? "HEAD"));

            if (exitCode != 0)
            {
                throw new GitTreeishException(
                    $"Failed to get commit hash for treeish {treeish ?? "master"} in {RepoPath}. Error message:\n{runner.Errors}");
            }

            return runner.Output.Trim();
        }

        public void Checkout(string treeish, bool track = false)
        {
            log.Info($"{"[" + ModuleName + "]",-30}Checkout {treeish}");

            var command = HasLocalBranch(treeish) || !track
                ? "git checkout " + treeish
                : $"git checkout -b {treeish} --track origin/{treeish}";

            var checkoutTask = runner.RunInDirectory(RepoPath, command, TimeSpan.FromMinutes(60));

            if (checkoutTask != 0)
            {
                log.Debug($"pull {ModuleName}");
                runner.RunInDirectory(RepoPath, "git pull", TimeSpan.FromMinutes(60));
                log.Debug($"pull result {ModuleName} {runner.Output}");

                checkoutTask = runner.RunInDirectory(RepoPath, command, TimeSpan.FromMinutes(60));
            }

            if (checkoutTask != 0)
            {
                var output = runner.Errors;
                throw new GitCheckoutException($"Failed to checkout to {treeish} from {ModuleName}. {output}");
            }
        }

        public void Fetch(string branch)
        {
            log.Info($"{"[" + ModuleName + "]",-30}Fetching {branch}");

            var exitCode = runner.RunInDirectory(RepoPath, "git fetch origin " + branch, TimeSpan.FromMinutes(60), RetryStrategy.IfTimeoutOrFailed);

            if (exitCode != 0)
            {
                throw new GitPullException($"Failed to fetch {RepoPath}:{branch}. Error message:\n{runner.Errors}");
            }
        }

        public void Pull(string treeish)
        {
            Fetch("");
            Merge("origin/" + treeish);
            Submodule.Update();
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
            log.Info($"{"[" + ModuleName + "]",-30}Getting treeish remote hash");
            var exitCode = runner.RunInDirectory(RepoPath, "git ls-remote origin " + treeish);

            if (exitCode != 0)
                throw new GitTreeishException("Fail get remote commit hash at " + ModuleName + "@" + treeish);

            return runner.Output.Split('\t')[0];
        }

        // ReSharper disable once UnusedMember.Global
        public void RewriteFileFromRemote(string branch, string shortPath, string destination)
        {
            log.Info($"{"[" + ModuleName + "]",-30}Rewrite file from remote {branch}:{shortPath}");

            shortPath = shortPath.Replace(Path.DirectorySeparatorChar.ToString(), "/");
            var exitCode = runner.RunInDirectory(
                RepoPath,
                $"git show origin/{branch}:{shortPath} > {destination}");

            if (exitCode != 0)
            {
                throw new GitRemoteException(
                    $"Failed to rewrite file from remote {RepoPath}:{branch}:{shortPath}. Error message:\n{runner.Errors}");
            }
        }

        public string ShowLocalChanges()
        {
            log.Info($"{"[" + ModuleName + "]",-30}Show local changes");
            var exitCode = runner.RunInDirectory(RepoPath, "git status -s");

            if (exitCode != 0)
            {
                throw new GitLocalChangesException($"Failed to get local changes in {RepoPath}. Error message:\n{runner.Errors}");
            }

            return runner.Output;
        }

        public bool HasLocalChanges()
        {
            return ShowLocalChanges().Split('\n').Any(line => line.Trim().Length > 0);
        }

        public IList<string> LocalBranches()
        {
            log.Info($"{"[" + ModuleName + "]",-30}Get local branches");
            var exitCode = runner.RunInDirectory(RepoPath, "git branch");

            if (exitCode != 0)
            {
                throw new GitBranchException($"Failed to get local branches in {RepoPath}. Error message:\n{runner.Errors}");
            }

            var lines = runner.Output.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            return lines.Select(l => l.Replace("*", "").Trim()).ToArray();
        }

        public void AddOrigin(string url)
        {
            log.Info($"{"[" + ModuleName + "]",-30}Add origin");
            var exitCode = runner.RunInDirectory(RepoPath, "git remote add origin " + url);

            if (exitCode != 0)
            {
                throw new GitCheckoutException($"Failed to add origin to {ModuleName}. Error message:\n{runner.Errors}");
            }
        }

        public void SetPushUrl(string url)
        {
            log.Info($"{"[" + ModuleName + "]",-30}Set push url");
            var exitCode = runner.RunInDirectory(RepoPath, "git remote set-url origin --push " + url);

            if (exitCode != 0)
            {
                throw new GitCheckoutException($"Failed to set push url origin to {ModuleName}. Error message:\n{runner.Errors}");
            }
        }

        public void DeleteUntrackedFiles()
        {
            log.Info($"{"[" + ModuleName + "]",-30}Deliting untaracked files");
            var exitCode = runner.RunInDirectory(RepoPath, "git clean -f -q");

            if (exitCode != 0)
            {
                throw new GitLocalChangesException($"Failed to clean local changes in {RepoPath}. Error message:\n{runner.Errors}");
            }
        }

        public void Clean()
        {
            log.Info($"{"[" + ModuleName + "]",-30}Clean and reset hard");
            var gitIgnore = Path.Combine(RepoPath, ".gitignore");
            if (File.Exists(gitIgnore))
                File.Delete(gitIgnore);
            log.Info($"{"[" + ModuleName + "]",-30}Remove from built cache");
            BuiltHelper.RemoveModuleFromBuiltInfo(ModuleName);

            var exitCode = runner.RunInDirectory(RepoPath, "git clean -f -d -q");
            if (exitCode != 0)
            {
                throw new GitLocalChangesException($"Failed to clean local changes in {RepoPath}. Error message:\n{runner.Errors}");
            }

            exitCode = runner.RunInDirectory(RepoPath, "git reset --hard");
            if (exitCode != 0)
            {
                throw new GitLocalChangesException($"Failed to reset local changes in {RepoPath}. Error message:\n{runner.Errors}");
            }
        }

        public IList<Branch> GetRemoteBranches()
        {
            log.Info($"{"[" + ModuleName + "]",-30}Get remote branches");
            var sw = Stopwatch.StartNew();
            var exitCode = runner.RunInDirectory(RepoPath, "git ls-remote --heads", TimeoutHelper.GetStartTimeout(), RetryStrategy.IfTimeoutOrFailed);

            sw.Stop();
            if (sw.Elapsed > TimeSpan.FromSeconds(10))
                log.DebugFormat("{0, -30}Elapsed git ls-remote --heads: [{1}]", "[" + ModuleName + "]", sw.Elapsed);

            if (exitCode != 0)
            {
                throw new GitBranchException(
                    $"Failed to get remote branches in {RepoPath}. Error message:\n{runner.Errors}");
            }

            var branches = runner.Output.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            return branches.Select(s => new Branch(s)).Where(b => b.Name != null).ToList();
        }

        public bool HasLocalBranch(string branch) =>
            LocalBranches().Contains(branch);

        public bool IsKnownRemoteBranch(string branch)
        {
            log.Info($"{"[" + ModuleName + "]",-30}Is known remote branch '{branch}'");
            var exitCode = runner.RunInDirectory(RepoPath, "git branch -r");
            if (exitCode != 0)
            {
                throw new GitBranchException(
                    $"Failed to get list of known remote branches in {RepoPath}. Error message:\n{runner.Errors}");
            }

            var knownRemoteBranches = runner.Output
                .Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Split('/').Last());
            return knownRemoteBranches.Contains(branch);
        }

        public void ResetHard(string treeish = null)
        {
            log.Info($"{"[" + ModuleName + "]",-30}Reset hard {treeish}");
            BuiltHelper.RemoveModuleFromBuiltInfo(ModuleName);
            runner.RunInDirectory(RepoPath, "git reset --hard " + (treeish == null ? "" : "origin/" + treeish));
        }

        public bool FastForwardPullAllowed(string treeish)
        {
            log.Info($"{"[" + ModuleName + "]",-30}Fast forward pull allowed for {treeish}");
            Fetch(treeish);
            var exitCode = runner.RunInDirectory(
                RepoPath,
                $"git merge-base {treeish} {"origin/" + treeish}");

            if (exitCode != 0)
            {
                throw new GitTreeishException($"Failed to get merge-base in {RepoPath}. Error message:\n{runner.Errors}");
            }

            var mergeBase = runner.Output.Trim();
            return mergeBase.Equals(CurrentLocalCommitHash()) || mergeBase.Equals(RemoteCommitHashAtBranch(treeish));
        }

        public bool BranchWasMerged(string treeish)
        {
            var exitCode = runner.RunInDirectory(RepoPath, "git branch -a --merged");

            if (exitCode != 0)
            {
                throw new GitBranchException($"Failed to get list of merged branches in {RepoPath}. Error message:\n{runner.Errors}");
            }

            var lines = runner.Output.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            return lines
                .Select(l => l.Replace("*", "").Trim())
                .Any(l => l.EndsWith("/" + treeish));
        }

        public void RemoteOriginUrls(out string fetchUrl, out string pushUrl)
        {
            log.Info($"{"[" + ModuleName + "]",-30}Remote origin URL");
            var exitCode = runner.RunInDirectory(RepoPath, "git remote -v");

            if (exitCode != 0)
            {
                throw new GitRemoteException($"Failed to get remote url in {RepoPath}. Error message:\n{runner.Errors}\n{runner.Output}");
            }

            var lines = runner.Output
                .Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .ToList();

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

            log.Info($"{"[" + ModuleName + "]",-30}Updating URL");
            ConsoleWriter.WriteInfo($"Update url for {RepoPath}: {fetchUrl} => {module.Url}");
            RemoveOrigin();
            AddOrigin(module.Url);

            if (module.Pushurl != null)
            {
                log.Info($"{"[" + ModuleName + "]",-30}Updating push URL");
                ConsoleWriter.WriteInfo($"Update push url for {RepoPath}: {pushUrl} => {module.Pushurl}");
                SetPushUrl(module.Pushurl);
            }
        }

        public List<string> GetFilesForCommit()
        {
            log.Info($"{"[" + ModuleName + "]",-30}Get files for commit");
            var exitCode = runner.RunInDirectory(RepoPath, "git diff --cached --name-only");

            if (exitCode != 0)
            {
                throw new GitCommitException($"Failed to get commit files in {RepoPath}. Error message:\n{runner.Errors}");
            }

            var lines = runner.Output.Split(new[] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
            return lines.ToList();
        }

        public string GetCommitInfo()
        {
            log.Info($"{"[" + ModuleName + "]",-30}Get commit info");
            var exitCode = runner.RunInDirectory(RepoPath, "git log HEAD -n 1 --date=relative --format=\"%ad\t\"%an\"\t<%ae>\"");

            if (exitCode != 0)
            {
                return "Failed to get commit info";
            }

            var tokens = runner.Output.Trim()
                .Split('\t')
                .ToArray();
            return $"{tokens[0],-25}" + tokens[1] + " " + tokens[2];
        }

        public void Commit(string[] args)
        {
            log.Info($"{"[" + ModuleName + "]",-30}git commit {args.Aggregate("", (x, y) => x + " \"" + y + "\"")}");

            var exitCode = runner.RunInDirectory(
                RepoPath,
                "git commit" + args.Aggregate("", (x, y) => x + " \"" + y + "\""));

            if (exitCode != 0)
            {
                throw new GitCommitException($"Failed to commit in {RepoPath}. Error:\n{runner.Output}");
            }
        }

        public string ShowUnpushedCommits()
        {
            log.Info($"{"[" + ModuleName + "]",-30}Show unpushed commits");

            var exitCode = runner.RunInDirectory(
                RepoPath,
                "git log --branches --not --remotes=origin --simplify-by-decoration --decorate --oneline");

            if (exitCode != 0)
            {
                throw new GitLocalChangesException($"Failed to get unpushed changes in {RepoPath}.");
            }

            return runner.Output;
        }

        public void Push(string branch)
        {
            log.Info($"{"[" + ModuleName + "]",-30}Push origin {branch}");

            var exitCode = runner.RunInDirectory(RepoPath, "git push origin " + branch, TimeSpan.FromMinutes(60));

            if (exitCode != 0)
            {
                throw new GitPushException($"Failed to push {RepoPath}:{branch}. Error:\n{runner.Output + runner.Errors}");
            }
        }

        private IList<Branch> RemoteBranches { get; set; }

        private void Merge(string treeish)
        {
            log.Info($"{"[" + ModuleName + "]",-30}Merge --ff-only '{treeish}'");
            var exitCode = runner.RunInDirectory(RepoPath, "git merge --ff-only " + treeish, TimeSpan.FromMinutes(60));

            if (exitCode != 0)
            {
                throw new GitPullException(
                    $"Failed to fast-forward pull in {RepoPath} for branch {treeish}. Error message:\n{runner.Errors}");
            }
        }

        private void RemoveOrigin()
        {
            log.Info($"{"[" + ModuleName + "]",-30}Remove origin");
            var exitCode = runner.RunInDirectory(RepoPath, "git remote rm origin ");

            if (exitCode != 0)
            {
                throw new GitCheckoutException($"Failed to remove in {ModuleName}. Error message:\n{runner.Errors}");
            }
        }
    }

    public class CurrentTreeish
    {
        public readonly string Value;
        public readonly TreeishType Type;

        public CurrentTreeish(TreeishType type, string value)
        {
            Value = value;
            Type = type;
        }
    }

    public class GitCommitException : CementException
    {
        public GitCommitException(string s)
            : base(s)
        {
        }
    }

    public class GitPushException : CementException
    {
        public GitPushException(string s)
            : base(s)
        {
        }
    }

    public class GitRemoteException : CementException
    {
        public GitRemoteException(string message)
            : base(message)
        {
        }
    }

    public class GitBranchException : CementException
    {
        public GitBranchException(string message)
            : base(message)
        {
        }
    }

    public class GitInitException : CementException
    {
        public GitInitException(string format)
            : base(format)
        {
        }
    }

    public class GitCloneException : CementException
    {
        public GitCloneException(string message)
            : base(message)
        {
        }
    }

    public class GitCheckoutException : CementException
    {
        public GitCheckoutException(string message)
            : base(message)
        {
        }
    }

    public class GitPullException : CementException
    {
        public GitPullException(string message)
            : base(message)
        {
        }
    }

    public class GitLocalChangesException : CementException
    {
        public GitLocalChangesException(string s)
            : base(s)
        {
        }
    }

    public class GitTreeishException : CementException
    {
        public GitTreeishException(string message)
            : base(message)
        {
        }
    }
}