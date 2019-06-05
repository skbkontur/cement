using System.Collections.Generic;
using Commands;
using Common;
using Common.YamlParsers;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.CommandsTests
{
    [TestFixture]
    public class TestDepsPrinter
    {
        [Test]
        public void TestSimpleTree()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B"), new Dep("C")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("D")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("D", null, "client")})}
                });
                env.CreateRepo("D", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep>())},
                    {"client", new DepsData(null, new List<Dep>())}
                });
                Helper.SetWorkspace(env.RemoteWorkspace);
                var result = new ShowDeps().GetDepsGraph(new Dep("A"));
                CollectionAssert.AreEqual(new[]
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
    }
}