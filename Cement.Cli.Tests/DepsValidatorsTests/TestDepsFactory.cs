using System;
using Common;

namespace Cement.Cli.Tests.DepsValidatorsTests;

public static class TestDepsFactory
{
    public static Dep[] GetUniqueDeps()
    {
        var deps = new Dep[Random.Shared.Next(10, 100)];
        for (var i = 0; i < deps.Length; i++)
            deps[i] = CreateDep();

        return deps;
    }

    public static Dep[] GetNonUniqueDeps()
    {
        var deps = new Dep[Random.Shared.Next(10, 100)];
        for (var i = 0; i < deps.Length - 2; i++)
            deps[i] = CreateDep();

        var dep = CreateDep();
        deps[^2] = dep;
        deps[^1] = dep;

        return deps;
    }

    private static Dep CreateDep(string name = default, string treeish = default, string configuration = default)
    {
        name ??= Guid.NewGuid().ToString();
        treeish ??= Guid.NewGuid().ToString();
        configuration ??= "full-build";

        return new Dep(name, treeish, configuration);
    }
}
