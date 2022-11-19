using System.Collections.Generic;
using Cement.Cli.Tests.Helpers;
using Cement.Cli.Commands;
using Cement.Cli.Common;
using Cement.Cli.Common.DepsValidators;
using Cement.Cli.Common.YamlParsers;
using NUnit.Framework;

namespace Cement.Cli.Tests.CommandsTests;

[TestFixture]
public class TestDepsPrinter
{
    private static readonly ConsoleWriter ConsoleWriter = ConsoleWriter.Shared;
    private static readonly DepsValidatorFactory DepsValidatorFactory = new();
    private static readonly FeatureFlags FeatureFlags = FeatureFlags.Default;

    [Test]
    public void TestSimpleTree()
    {
        using var env = new TestEnvironment();
        env.CreateRepo(
            "A", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("B"), new("C")})}
            });
        env.CreateRepo(
            "B", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("D")})}
            });
        env.CreateRepo(
            "C", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("D", null, "client")})}
            });
        env.CreateRepo(
            "D", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep>())},
                {"client", new DepsData(null, new List<Dep>())}
            });
        Helper.SetWorkspace(env.RemoteWorkspace);
        var result = new ShowDepsCommand(ConsoleWriter, FeatureFlags, DepsValidatorFactory).GetDepsGraph(new Dep("A"));
        CollectionAssert.AreEqual(
            new[]
            {
                ";Copy paste this text to http://arborjs.org/halfviz/#",
                "{color:DimGrey}",
                "A/full-build {color:red}",
                "B/full-build {color:green}",
                "C/full-build {color:green}",
                "A/full-build -> B/full-build",
                "A/full-build -> C/full-build",
                "B/full-build -> D/full-build",
                "C/full-build -> D/client"
            }, result);
    }
}
