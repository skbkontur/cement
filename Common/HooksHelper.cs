using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Extensions;
using Common.Logging;
using Common.YamlParsers;
using Microsoft.Extensions.Logging;

namespace Common
{
    public static class HooksHelper
    {
        private static readonly ILogger Log = LogManager.GetLogger(typeof(HooksHelper));
        private const string CementPreCommitHookName = "pre-commit.cement";

        public static bool InstallHooks(string moduleName)
        {
            if (!Yaml.Exists(moduleName))
                return false;
            var hooks = Yaml.HooksParser(moduleName).Get();
            if (!hooks.Any())
                return false;

            var gitFolder = Path.Combine(Helper.CurrentWorkspace, moduleName, ".git");
            var gitHooksFolder = Path.Combine(gitFolder, "hooks");
            if (!GitFolderExists(moduleName, gitFolder))
                return false;
            if (!Directory.Exists(gitHooksFolder))
                Directory.CreateDirectory(gitHooksFolder);
            CreateCementHook(gitHooksFolder);

            if (!IsUniqueHooks(moduleName, hooks))
                return false;

            var updated = false;
            foreach (var hook in hooks)
                updated |= InstallHook(moduleName, hook, gitHooksFolder);
            return updated;
        }

        private static bool IsUniqueHooks(string moduleName, List<string> hooks)
        {
            if (hooks.Contains(CementPreCommitHookName) && hooks.Contains("pre-commit"))
            {
                ConsoleWriter.WriteError($"You can't use {CementPreCommitHookName} with custom pre-commit hook in {moduleName}");
                ConsoleWriter.WriteLine(@"if you want to use cement hook, add this to your bash hook:
.git/hooks/pre-commit.cement
if [ $? -ne 0 ]; then
  exit 1
fi
");
                Log.LogError("Cement hook with pre-commit found in " + moduleName);
                return false;
            }

            if (hooks.Distinct().Count() == hooks.Count)
                return true;

            Log.LogError("Duplicate hook in " + moduleName);
            ConsoleWriter.WriteError("Duplicate git hook found in " + moduleName + " module");
            return false;
        }

        private static bool GitFolderExists(string moduleName, string gitFolder)
        {
            if (Directory.Exists(gitFolder))
                return true;

            ConsoleWriter.WriteWarning(".git folder not found at " + moduleName);
            return false;
        }

        private static bool InstallHook(string moduleName, string hook, string gitHooksFolder)
        {
            Log.LogDebug("installing hook " + hook + " into " + moduleName);

            string hookName, hookSrc;
            if (hook == CementPreCommitHookName)
            {
                hookName = "pre-commit";
                hookSrc = Path.Combine(gitHooksFolder, hook);
            }
            else
            {
                hookName = Path.GetFileName(hook);
                hookSrc = Path.Combine(Helper.CurrentWorkspace, moduleName, hook);
            }

            var hookDst = Path.Combine(gitHooksFolder, hookName);
            return CopyHook(hookSrc, hookDst);
        }

        private static bool CopyHook(string hookSrc, string hookDst)
        {
            if (!File.Exists(hookSrc))
            {
                ConsoleWriter.WriteWarning("Hook " + hookSrc + " not found.");
                return false;
            }

            var updated = IsHookChange(hookSrc, hookDst);
            File.Copy(hookSrc, hookDst, true);
            return updated;
        }

        private static bool IsHookChange(string hookSrc, string hookDst)
        {
            if (!File.Exists(hookDst))
                return true;

            if (File.ReadAllText(hookSrc) == File.ReadAllText(hookDst))
                return false;

            var hookBackup = hookDst + "." + DateTime.Now.ToString("dd.MM.yyyy");
            File.Copy(hookDst, hookBackup, true);
            return true;
        }

        private static void CreateCementHook(string hookFolder)
        {
            const string hookText = @"#!/bin/sh
#cement pre-commit 0.1

errors=$(cm check-pre-commit | wc -c)

if [ $errors -gt 0 ]; then
  cm check-pre-commit
  exit 1
fi
";

            var hookFile = Path.Combine(hookFolder, CementPreCommitHookName);
            File.WriteAllText(hookFile, hookText);
        }
    }
}