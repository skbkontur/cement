using System;
using System.Linq;
using Cement.Cli.Common.Exceptions;

namespace Cement.Cli.Common;

public sealed class Branch
{
    private const string RefHeadsPrefix = "refs/heads/";

    private Branch(string commitHash, string name)
    {
        CommitHash = commitHash;
        Name = name;
    }

    public static Branch Parse(string branchDescription)
    {
        var tokens = branchDescription
            .Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries)
            .ToArray();

        if (tokens.Length < 2 || tokens[1].IndexOf(RefHeadsPrefix, StringComparison.Ordinal) < 0)
        {
            throw new GitBranchException("Can't parse branch from:\n" + branchDescription);
        }

        var commitHash = tokens[0];
        var name = tokens[1].Replace(RefHeadsPrefix, string.Empty);

        return new Branch(commitHash, name);
    }

    public string CommitHash { get; }
    public string Name { get; }
}
