using System;
using System.Text;
using Common.Exceptions;
using JetBrains.Annotations;
using log4net;

namespace Common
{
    [PublicAPI]
    public sealed class GitSubmodule
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

        private readonly ILog log;
        private readonly GitRepository repository;
        private readonly ShellRunner shellRunner;

        public GitSubmodule([NotNull] ILog log, [NotNull] GitRepository repository, [NotNull] ShellRunner shellRunner)
        {
            this.log = log ?? throw new ArgumentNullException(nameof(log));
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.shellRunner = shellRunner ?? throw new ArgumentNullException(nameof(shellRunner));
        }

        /// <summary>
        /// Add submodule
        /// </summary>
        /// <param name="submoduleUrl">Absolute or relative URL of the project you would like to start tracking</param>
        /// <param name="branch"></param>
        /// <param name="directory"></param>
        /// <exception cref="GitSubmoduleAddException"></exception>
        public void Add(string submoduleUrl, string branch = null, string directory = null)
        {
            log.Info($"{"[" + repository.ModuleName + "]",-30}Adding submodule");

            branch = string.IsNullOrWhiteSpace(branch) ? "master" : branch;
            directory = string.IsNullOrWhiteSpace(directory) ? "src/submodules" : directory;

            var commandBuilder = new StringBuilder("git submodule add");
            commandBuilder.AppendFormat(" --branch {0}", branch);
            commandBuilder.Append(" --");
            commandBuilder.AppendFormat(" {0}", submoduleUrl);
            commandBuilder.AppendFormat(" \"{0}\"", directory);

            var command = commandBuilder.ToString();

            log.Debug($"{"[" + repository.ModuleName + "]",-30}command: '{command}'");

            var exitCode = shellRunner.RunInDirectory(repository.RepoPath, command, DefaultTimeout);

            if (exitCode == 0)
                return;

            var errorMessage = $"Failed to add submodule. Error message:\n{shellRunner.Errors}";

            log.Error(errorMessage);
            throw new GitSubmoduleAddException(errorMessage);
        }

        /// <summary>
        /// Initialize local configuration file
        /// </summary>
        /// <exception cref="GitSubmoduleInitException"></exception>
        public void Init()
        {
            log.Info($"{"[" + repository.ModuleName + "]",-30}Initializing submodule");

            const string command = "git submodule init";
            var exitCode = shellRunner.RunInDirectory(repository.RepoPath, command, DefaultTimeout);

            if (exitCode == 0)
                return;

            var errorMessage = $"Failed to initialize submodule. Error message:\n{shellRunner.Errors}";

            log.Error(errorMessage);
            throw new GitSubmoduleInitException(errorMessage);
        }

        /// <summary>
        /// Fetch all the data from submodule repository
        /// </summary>
        /// <exception cref="GitSubmoduleUpdateException"></exception>
        public void Update()
        {
            log.Info($"{"[" + repository.ModuleName + "]",-30}Updating submodule");

            const string command = "git submodule update --remote --recursive";
            var exitCode = shellRunner.RunInDirectory(repository.RepoPath, command, DefaultTimeout);

            if (exitCode == 0)
                return;

            var errorMessage = $"Failed to update submodule. Error message:\n{shellRunner.Errors}";

            log.Error(errorMessage);
            throw new GitSubmoduleUpdateException(errorMessage);
        }
    }
}