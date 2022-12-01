using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cement.Cli.Common;
using Cement.Cli.Common.ArgumentsParsing;
using Cement.Cli.Common.DepsValidators;
using Cement.Cli.Common.Exceptions;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class UsagesGrepCommand : Command<UsagesGrepCommandOptions>
{
    private static readonly CommandSettings Settings = new()
    {
        MeasureElapsedTime = true,
        Location = CommandLocation.RootModuleDirectory
    };
    private static readonly string[] GrepParametersWithValue =
    {
        "-A", "-B", "-C", "--threads", "-f",
        "-e", "--parent-basename", "--max-depth"
    };
    private static readonly string[] NewLine = {"\r\n", "\r", "\n"};
    private static readonly Regex Whitespaces = new("\\s");
    private readonly ConsoleWriter consoleWriter;
    private readonly CycleDetector cycleDetector;
    private readonly ShellRunner shellRunner;
    private readonly IUsagesProvider usagesProvider;
    private readonly HooksHelper hooksHelper;
    private readonly IDepsValidatorFactory depsValidatorFactory;
    private readonly IGitRepositoryFactory gitRepositoryFactory;

    private string moduleName;
    private string cwd;
    private string workspace;
    private GitRepository currentRepository;

    public UsagesGrepCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags, CycleDetector cycleDetector,
                             IDepsValidatorFactory depsValidatorFactory, IGitRepositoryFactory gitRepositoryFactory,
                             ShellRunner shellRunner, IUsagesProvider usagesProvider, HooksHelper hooksHelper)
        : base(consoleWriter, Settings, featureFlags)
    {
        this.consoleWriter = consoleWriter;
        this.cycleDetector = cycleDetector;
        this.depsValidatorFactory = depsValidatorFactory;
        this.gitRepositoryFactory = gitRepositoryFactory;
        this.shellRunner = shellRunner;
        this.usagesProvider = usagesProvider;
        this.hooksHelper = hooksHelper;
    }

    public override string Name => "grep";
    public override string HelpMessage => @"";

    protected override UsagesGrepCommandOptions ParseArgs(string[] args)
    {
        var parsed = ArgumentParser.ParseGrepParents(args);
        var arguments = (string[])parsed["gitArgs"];
        var fileMasks = (string[])parsed["fileMaskArgs"];

        var checkingBranch = (string)parsed["branch"];
        var skipGet = (bool)parsed["skip-get"];

        return new UsagesGrepCommandOptions(arguments, fileMasks, skipGet, checkingBranch);
    }

    protected override int Execute(UsagesGrepCommandOptions options)
    {
        cwd = Directory.GetCurrentDirectory();
        workspace = Directory.GetParent(cwd).FullName;
        moduleName = Path.GetFileName(cwd);
        currentRepository = gitRepositoryFactory.Create(moduleName, workspace);

        var checkingBranch = options.CheckingBranch;
        if (checkingBranch == null)
            checkingBranch = currentRepository.HasLocalBranch("master") ? "master" : currentRepository.CurrentLocalTreeish().Value;

        var response = usagesProvider.GetUsages(moduleName, checkingBranch);

        var usages = response.Items
            .SelectMany(kvp => kvp.Value)
            .Where(d => d.Treeish == "master")
            .DistinctBy(d => d.Name);

        Grep(options.Arguments, options.FileMasks, options.SkipGet, usages);
        return 0;
    }

    private static string BuildGitCommand(string[] args, string[] masks)
    {
        var sb = new StringBuilder();

        sb.Append("git --no-pager grep -n ");
        for (var i = 2; i < args.Length; ++i)
        {
            if (args[i - 1] == "-f")
                sb.Append('"').Append(Path.GetFullPath(args[i])).Append('"');
            else
            {
                if (IsPatternWithoutFlag(args, i))
                    sb.Append("-e ");
                sb.Append(Escape(Whitespaces.Replace(args[i], "\\s")));
            }

            sb.Append(' ');
        }

        if (masks.Length > 0)
            sb.Append("-- ");
        foreach (var mask in masks)
            sb.Append('"').Append(mask).Append('"');

        return sb.ToString();
    }

    private static bool IsPatternWithoutFlag(string[] args, int position)
    {
        return !(args[position][0] == '-' || GrepParametersWithValue.Contains(args[position - 1]));
    }

    private static string Escape(string s)
    {
        return s.Replace("\"", "\\\"");
    }

    private void Grep(string[] arguments, string[] fileMasks, bool skipGet, IEnumerable<Dep> toGrep)
    {
        var modules = Helper.GetModules();
        var command = BuildGitCommand(arguments, fileMasks);
        consoleWriter.WriteInfo(command);
        consoleWriter.WriteLine();

        using (new DirectoryJumper(workspace))
        {
            var clonedModules = skipGet
                ? GetExistingDirectories(toGrep)
                : CloneModules(toGrep, modules);

            foreach (var module in clonedModules)
            {
                // todo(dstarasov): не проверяется exitCode
                var (_, output, _) = shellRunner.RunInDirectory(module, command);
                if (string.IsNullOrWhiteSpace(output))
                    continue;

                consoleWriter.WriteLine(AddModuleToOutput(output, module));
                consoleWriter.WriteLine();
            }
        }
    }

    private List<string> GetExistingDirectories(IEnumerable<Dep> toGrep)
    {
        return toGrep.Select(d => d.Name).Where(Directory.Exists).ToList();
    }

    private List<string> CloneModules(IEnumerable<Dep> toGrep, List<Module> modules)
    {
        var clonedModules = new List<string>();
        foreach (var depParent in toGrep)
        {
            try
            {
                GetWithoutDependencies(depParent, modules);
                clonedModules.Add(depParent.Name);
            }
            catch (CementException exception)
            {
                consoleWriter.WriteError(exception.ToString());
            }
        }

        return clonedModules;
    }

    private string AddModuleToOutput(string output, string module)
    {
        var lines = output.Split(NewLine, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < lines.Length; ++i)
            lines[i] = module + "/" + lines[i];
        return string.Join(Environment.NewLine, lines);
    }

    private void GetWithoutDependencies(Dep dep, List<Module> modules)
    {
        var getter = new ModuleGetter(
            consoleWriter,
            cycleDetector,
            depsValidatorFactory,
            gitRepositoryFactory,
            hooksHelper,
            modules,
            dep,
            LocalChangesPolicy.FailOnLocalChanges,
            null);

        getter.GetModule();
    }
}
