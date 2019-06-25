using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common.YamlParsers;
using log4net;

namespace Common
{
    public class ModuleGetter
    {
        private readonly List<Module> modules;
        private readonly Dep rootModule;
        private readonly LocalChangesPolicy userLocalChangesPolicy;
        private bool errorOnMerge;
        private readonly bool localBranchForce;
        private readonly bool verbose;
        private static readonly ILog Log = new PrefixAppender("MODULE-GETTER", LogManager.GetLogger(typeof(ModuleGetter)));
        private readonly string mergedBranch;
        private GitRepository rootRepo;
        private string rootRepoTreeish;

        public ModuleGetter(List<Module> modules, Dep rootModule,
            LocalChangesPolicy userLocalChangesPolicy,
            string mergedBranch, bool verbose = false, bool localBranchForce = false)
        {
            this.modules = modules;
            this.rootModule = rootModule;
            this.userLocalChangesPolicy = userLocalChangesPolicy;
            this.localBranchForce = localBranchForce;
            this.verbose = verbose;
            this.mergedBranch = mergedBranch;
        }

        public void GetModule()
        {
            GetModule(rootModule, null);
        }

        public void GetDeps()
        {
            rootRepo = new GitRepository(rootModule.Name, Helper.CurrentWorkspace, Log);
            rootRepoTreeish = rootRepo.CurrentLocalTreeish().Value;

            var depsContent = new DepsParser(rootRepo.RepoPath).Get(rootModule.Configuration);
            depsContent.Force = depsContent.Force?.Select(f => Helper.DefineForce(f, rootRepo)).ToArray();
            Log.Info("OK");

            var queue = new DepsQueue();
            queue.AddRange(depsContent.Deps, rootModule.Name);

            var proceed = new ModulesContainer();
            GetDeps(depsContent.Force, queue, proceed);

            CycleDetector.WarnIfCycle(rootModule.Name, rootModule.Configuration, Log);
        }

        private void TakeDepsToProcessFromQueue(DepsQueue queue, IList<DepWithParent> depsPool,
            IList<DepWithParent> backToQueue, ModulesContainer processed)
        {
            while (!queue.IsEmpty())
            {
                var curDep = queue.Pop();
                if (curDep.Dep.Treeish == null)
                    curDep.Dep.Treeish = processed.GetTreeishByName(curDep.Dep.Name);
                if (processed.IsProcessed(curDep.Dep))
                    continue;
                processed.ThrowOnTreeishConflict(curDep);
                if (depsPool.Select(d => d.Dep.Name).Contains(curDep.Dep.Name))
                    backToQueue.Add(curDep);
                else
                    depsPool.Add(curDep);
            }
        }

        private void GetDeps(string[] force, DepsQueue queue, ModulesContainer processed)
        {
            while (!queue.IsEmpty())
            {
                var depsPool = new List<DepWithParent>();
                var backToQueue = new List<DepWithParent>();
                TakeDepsToProcessFromQueue(queue, depsPool, backToQueue, processed);

                foreach (var dep in depsPool)
                    processed.AddConfig(dep.Dep.Name, dep.Dep.Configuration);
                queue.AddRange(backToQueue);
                ProcessDeps(force, depsPool);

                MarkProcessedDeps(queue, processed, depsPool);
                AddNewDeps(queue, processed, depsPool);
            }

            if (errorOnMerge)
                ConsoleWriter.WriteWarning($"Branch '{mergedBranch}' was not merged into some of dependencies");
        }

        private void ProcessDeps(string[] force, List<DepWithParent> depsPool)
        {
            if (depsPool.Any())
                Log.Info("Parallel update-deps iteration: " + depsPool.Select(d => d.Dep.ToString() + "(" + d.ParentModule + ")").Aggregate((a, b) => a + " " + b));
            try
            {
                Parallel.ForEach(depsPool.Where(d => d.Dep.Name != rootModule.Name), Helper.ParallelOptions, d => GetModule(d.Dep, force));
            }
            catch (AggregateException ae)
            {
                throw ae.Flatten().InnerExceptions.First();
            }
        }

        private static void AddNewDeps(DepsQueue queue, ModulesContainer processed, List<DepWithParent> depsPool)
        {
            foreach (var dep in depsPool)
            {
                var currentModuleDeps = GetCurrentModuleDeps(dep.Dep);

                if (currentModuleDeps.Deps == null)
                    continue;

                queue.AddRange(currentModuleDeps.Deps.Where(d => !processed.IsProcessed(d))
                    .Select(d =>
                    {
                        d.NeedSrc = processed.IsNeedSrc(d.Name);
                        return d;
                    })
                    .Select(d => new DepWithParent(d, dep.Dep.Name)).ToList());
            }
        }

        private static void MarkProcessedDeps(DepsQueue queue, ModulesContainer processed, List<DepWithParent> depsPool)
        {
            foreach (var dep in depsPool)
            {
                dep.Dep.UpdateConfigurationIfNull();
                if (dep.Dep.Treeish != null && processed.GetDepsByName(dep.Dep.Name).All(d => d.Treeish == null))
                {
                    processed.ChangeTreeish(dep.Dep.Name, dep.Dep.Treeish);
                    Log.Info("Need get " + dep.Dep.Name + " again");
                    queue.AddRange(processed.GetConfigsByName(dep.Dep.Name).Select(c =>
                        new DepWithParent(new Dep(dep.Dep.Name, null, c), dep.ParentModule)).ToList());
                }
                if (dep.Dep.NeedSrc)
                    processed.SetNeedSrc(dep.Dep.Name);
                processed.Add(dep);
            }
        }

        private static DepsData GetCurrentModuleDeps(Dep dep)
        {
            Log.Info($"{"[" + dep.Name + "]",-30}Getting deps for configuration {dep.Configuration ?? "full-build"}");
            var deps = new DepsParser(Path.Combine(Helper.CurrentWorkspace, dep.Name)).Get(dep.Configuration);
            return deps;
        }

        private void GetModule(Dep dep, string[] force)
        {
            Log.Info($"{"[" + dep.Name + "]",-30}Update '{dep.Treeish ?? "master"}'");
            if (dep.Treeish == "$CURRENT_BRANCH")
            {
                force = new[] {Helper.DefineForce(dep.Treeish, rootRepoTreeish)};
                dep.Treeish = null;
            }
            ConsoleWriter.WriteProgress(dep.Name + "   " + dep.Treeish);

            var module = modules.FirstOrDefault(m => m.Name.Equals(dep.Name));
            if (module == null)
                throw new CementException("Failed to find module " + dep.Name);

            GetFullModule(dep, force);
        }

        private void GetFullModule(Dep dep, string[] force)
        {
            var getInfo = new GetInfo();
            var module = modules.FirstOrDefault(m => m.Name.Equals(dep.Name));
            if (module == null)
                throw new CementException("Failed to find module " + dep.Name);

            var repo = new GitRepository(dep.Name, Helper.CurrentWorkspace, Log);
            if (!repo.IsGitRepo)
            {
                ConsoleWriter.WriteProgress(dep.Name + "   " + dep.Treeish + " cloning");
                if (Directory.Exists(Path.Combine(repo.Workspace, repo.ModuleName)))
                    CloneInNotEmptyFolder(module, repo);
                else
                    CloneInEmptyFolder(dep, module, repo);
                getInfo.Cloned = true;
            }
            repo.TryUpdateUrl(modules.FirstOrDefault(m => m.Name.Equals(dep.Name)));

            GetTreeish(repo, dep, force, dep.Treeish, getInfo);
            if (verbose)
                getInfo.CommitInfo = repo.GetCommitInfo();
            if (HooksHelper.InstallHooks(dep.Name))
                getInfo.HookUpdated = true;
            PrintProcessedModuleInfo(dep, getInfo, getInfo.Forced ? getInfo.ForcedBranch : dep.Treeish);
            WarnIfNotMerged(repo);
        }

        private static void CloneInNotEmptyFolder(Module module, GitRepository repo)
        {
            repo.Init();
            repo.AddOrigin(module.Url);
            if (module.Pushurl != null)
                repo.SetPushUrl(module.Pushurl);
            repo.Fetch("");
            repo.ResetHard("master");
            repo.DeleteUntrackedFiles();
        }

        private static void CloneInEmptyFolder(Dep dep, Module module, GitRepository repo)
        {
            if (GitRepository.HasRemoteBranch(module.Url, dep.Treeish))
            {
                repo.Clone(module.Url, dep.Treeish);
            }
            else
            {
                repo.Clone(module.Url);
            }

            if (module.Pushurl != null)
                repo.SetPushUrl(module.Pushurl);
        }

        private void WarnIfNotMerged(GitRepository repo)
        {
            if (mergedBranch == null)
                return;
            if (repo.HasLocalBranch(mergedBranch) || repo.HasRemoteBranch(mergedBranch))
            {
                if (repo.BranchWasMerged(mergedBranch))
                {
                    ConsoleWriter.WriteOk($"Branch '{mergedBranch}' was merged into {repo.ModuleName}");
                }
                else
                {
                    ConsoleWriter.WriteWarning($"Branch '{mergedBranch}' was not merged into {repo.ModuleName}");
                    errorOnMerge = true;
                }
            }
        }

        private string GetPrintString(int longestModuleNameLength, string moduleName, string treeishInfo, string commitInfo)
        {
            return string.Format(@"   {0, -" + longestModuleNameLength + "}    {1, -18}    {2}", moduleName,
                treeishInfo, commitInfo);
        }

        private void PrintProcessedModuleInfo(Dep dep, GetInfo getInfo, string treeish)
        {
            var longestModuleName = modules.Select(m => m.Name.Length).Max() + 5;
            dep.UpdateConfigurationIfNull();
            var name = dep.Name + (dep.Configuration == null || dep.Configuration.Equals("full-build") ? "" : Helper.ConfigurationDelimiter + dep.Configuration);
            var outputTreeish = getInfo.Forced && !treeish.Equals("master") ? treeish + " *forced" : (dep.Treeish ?? "master");

            if (getInfo.HookUpdated)
                outputTreeish += " (hook updated) ";

            if (getInfo.Pulled)
            {
                outputTreeish += " *pulled";
                ConsoleWriter.WriteUpdate(GetPrintString(longestModuleName, name, outputTreeish, getInfo.CommitInfo));
                Log.Debug($"{"[" + dep.Name + "]",-30}{outputTreeish,-18}    {getInfo.CommitInfo}");
                return;
            }
            if (getInfo.ForcedLocal)
            {
                outputTreeish += " *forced local";
                ConsoleWriter.WriteWarning(GetPrintString(longestModuleName, name, outputTreeish, getInfo.CommitInfo));
                Log.Debug($"{"[" + dep.Name + "]",-30}{outputTreeish,-18}    {getInfo.CommitInfo}");
                return;
            }
            if (getInfo.Reset)
            {
                outputTreeish += " *reset";
                ConsoleWriter.WriteWarning(GetPrintString(longestModuleName, name, outputTreeish, getInfo.CommitInfo));
                Log.Debug($"{"[" + dep.Name + "]",-30}{outputTreeish,-18}    {getInfo.CommitInfo}");
                return;
            }
            if (getInfo.Changed || getInfo.Cloned)
            {
                Log.Debug(
                    $"{"[" + dep.Name + "]",-30}{outputTreeish + (getInfo.Cloned ? " *cloned" : " *changed"),-18}    {getInfo.CommitInfo}");
                ConsoleWriter.WriteUpdate(GetPrintString(longestModuleName, name, outputTreeish, getInfo.CommitInfo));
            }
            else
            {
                Log.Debug($"{"[" + dep.Name + "]",-30}{outputTreeish + " *skipped",-18}    {getInfo.CommitInfo}");
                ConsoleWriter.WriteSkip(GetPrintString(longestModuleName, name, outputTreeish, getInfo.CommitInfo));
            }
        }

        private void GetTreeish(GitRepository repo, Dep dep, string[] force, string treeish, GetInfo getInfo)
        {
            treeish = treeish ?? "master";
            Log.Info($"{"[" + dep.Name + "]",-30}Getting treeish '{treeish}'");

            var hasRemoteBranch = repo.HasRemoteBranch(treeish);
            getInfo.ForcedBranch = HaveToForce(dep, force, repo);
            if (getInfo.Forced)
            {
                treeish = getInfo.ForcedBranch;
                Log.Info($"{"[" + dep.Name + "]",-30}treeish '{treeish}' was forced");
            }

            ConsoleWriter.WriteProgress(dep.Name + "/" + dep.Configuration + "\t" + treeish);

            var oldSha = repo.SafeGetCurrentLocalCommitHash();
            var remoteSha = repo.HasRemoteBranch(treeish) ? repo.RemoteCommitHashAtBranch(treeish) : treeish;

            var localChangesAction = DefineLocalChangesPolicy(repo, oldSha, remoteSha);

            if (localChangesAction == LocalChangesAction.ForceLocal)
            {
                getInfo.ForcedLocal = true;
                return;
            }

            if (localChangesAction == LocalChangesAction.Reset)
            {
                Reset(repo, dep);
                getInfo.Reset = true;
            }

            ConsoleWriter.WriteProgress(dep.Name + " looking for remote commit hash");
            if (hasRemoteBranch && repo.RemoteCommitHashAtBranch(treeish).Equals(oldSha) && repo.CurrentLocalTreeish().Value.Equals(treeish))
            {
                return;
            }

            if (repo.HasLocalBranch(treeish))
            {
                ConsoleWriter.WriteProgress(dep.Name + " has local branch " + treeish);
                Log.Info($"{"[" + dep.Name + "]",-30}has local branch '{treeish}'");

                ConsoleWriter.WriteProgress(dep.Name + " checkout " + treeish);
                repo.Checkout(treeish);
                if (hasRemoteBranch)
                {
                    if (userLocalChangesPolicy == LocalChangesPolicy.Reset && !repo.FastForwardPullAllowed(treeish))
                        repo.ResetHard(treeish);
                    else
                    {
                        ConsoleWriter.WriteProgress(dep.Name + " pull " + treeish);
                        repo.Pull(treeish);
                    }
                }
            }
            else
            {
                Log.Info($"{"[" + dep.Name + "]",-30}doesn't have local branch '{treeish}'");
                ConsoleWriter.WriteProgress(dep.Name + " fetch " + treeish);
                if (repo.HasRemoteBranch(treeish))
                    repo.Fetch(treeish);
                else
                    repo.Fetch("");
                ConsoleWriter.WriteProgress(dep.Name + " checkout " + treeish);
                repo.Checkout(treeish);
            }

            var newSha = repo.CurrentLocalCommitHash();
            getInfo.Reset = false;

            if (userLocalChangesPolicy == LocalChangesPolicy.Reset && hasRemoteBranch &&
                !repo.RemoteCommitHashAtBranch(treeish).Equals(newSha))
            {
                repo.ResetHard(treeish);
                getInfo.Reset = true;
            }
            getInfo.Changed = !oldSha.Equals(newSha);
            getInfo.Pulled = localChangesAction == LocalChangesAction.Pull;
        }

        private static void Reset(GitRepository repo, Dep dep)
        {
            int times = 3;
            for (int i = 0; i < times; i++)
            {
                try
                {
                    ConsoleWriter.WriteProgress(dep.Name + " cleaning");
                    repo.Clean();
                    ConsoleWriter.WriteProgress(dep.Name + " resetting");
                    repo.ResetHard();
                    Log.Info($"{"[" + dep.Name + "]",-30}Reseted in {i + 1} times");
                    return;
                }
                catch (Exception)
                {
                    if (i + 1 == times)
                        throw;
                }
            }
        }

        private string HaveToForce(Dep dep, string[] force, GitRepository repo)
        {
            if (force != null)
                foreach (var f in force)
                {
                    if (!localBranchForce && repo.HasLocalBranch(f) && !repo.HasRemoteBranch(f))
                    {
                        ConsoleWriter.WriteWarning(
                            $"Module '{repo.ModuleName}' has local-only branch '{f}' which will not be forced.\nUse --allow-local-branch-force key to force it");
                        continue;
                    }

                    if (dep.Treeish == null && repo.HasRemoteBranch(f))
                        return f;
                }
            return null;
        }

        private LocalChangesAction DefineLocalChangesPolicy(GitRepository repo, string localSha, string remoteSha)
        {
            if (userLocalChangesPolicy == LocalChangesPolicy.Reset && localSha != remoteSha)
                return LocalChangesAction.Reset;

            if (!repo.HasLocalChanges())
            {
                return LocalChangesAction.Nothing;
            }
            ConsoleWriter.WriteWarning($"Local changes found in '{repo.RepoPath}'\n{repo.ShowLocalChanges()}");
            switch (userLocalChangesPolicy)
            {
                case LocalChangesPolicy.FailOnLocalChanges:
                    throw new GitLocalChangesException("Failed to update " + repo.RepoPath + " due to local changes.");
                case LocalChangesPolicy.ForceLocal:
                    return LocalChangesAction.ForceLocal;
                case LocalChangesPolicy.Pull:
                    return LocalChangesAction.Pull;
                case LocalChangesPolicy.Reset:
                    return LocalChangesAction.Reset;
                case LocalChangesPolicy.Interactive:
                    Console.WriteLine("What do you want to do? enter 'f' for saving local changes / 'r' for resetting local changes(git clean & git reset) / 'p' for pull anyway :\n");
                    return GetUserAnswer();
            }
            return LocalChangesAction.Nothing;
        }

        private static LocalChangesAction GetUserAnswer()
        {
            var userActions = new Dictionary<string, LocalChangesAction>
            {
                {"r", LocalChangesAction.Reset},
                {"f", LocalChangesAction.ForceLocal},
                {"p", LocalChangesAction.Pull}
            };
            while (true)
            {
                var answer = Console.ReadLine();
                if (answer == null)
                {
                    Console.WriteLine("Unknown choice. Try again");
                    continue;
                }
                answer = answer.Trim().ToLower();
                if (userActions.ContainsKey(answer))
                    return userActions[answer];
                Console.WriteLine("Unknown choice. Try again");
            }
        }
    }

    public enum LocalChangesAction
    {
        Nothing,
        Reset,
        ForceLocal,
        Pull
    }

    public enum LocalChangesPolicy
    {
        FailOnLocalChanges,
        Reset,
        ForceLocal,
        Pull,
        Interactive
    }

    public class GetInfo
    {
        public bool Cloned;
        public bool Forced => ForcedBranch != null;
        public string ForcedBranch;
        public bool Changed;
        public bool ForcedLocal;
        public bool Pulled;
        public bool Reset;
        public string CommitInfo;
        public bool HookUpdated;

        public GetInfo()
        {
            Cloned = false;
            ForcedBranch = null;
            Changed = false;
            ForcedLocal = false;
            Pulled = false;
            Reset = false;
            HookUpdated = false;
            CommitInfo = "";
        }
    }
}