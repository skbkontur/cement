using Common;
using NUnit.Framework;

namespace Tests.ParsersTests
{
	[TestFixture]
	public class TestParseIniDeps
	{
		[Test]
		public void TestParseDeps()
		{
			var content = @"
[main]
force = 
 A -> B
 C -> B
 $CURRENT_BRANCH
[module A]
treesh = develop
[module B]
treeish = b
[module C]
treeish = c
conf = client
[module D]
treesh = d
config = sdk
#comment";
			var depsContent = new DepsIniParser(content).Get();
			var deps = depsContent.Deps;
			var expectedForce = @"
A -> B
C -> B
$CURRENT_BRANCH";
			Assert.AreEqual(expectedForce, depsContent.Force);
			Assert.AreEqual(4, deps.Count);
			Assert.AreEqual("A", deps[0].Name);
			Assert.AreEqual("develop", deps[0].Treeish);
			Assert.AreEqual("B", deps[1].Name);
			Assert.AreEqual("b", deps[1].Treeish);
			Assert.AreEqual("C", deps[2].Name);
			Assert.AreEqual("c", deps[2].Treeish);
			Assert.AreEqual("client", deps[2].Configuration);
			Assert.AreEqual("D", deps[3].Name);
			Assert.AreEqual("d", deps[3].Treeish);
			Assert.AreEqual("sdk", deps[3].Configuration);
		}

		[Test]
		public void TestEmptyDeps()
		{
			var content = @"
";
			var depsContent = new DepsIniParser(content).Get();
			var deps = depsContent.Deps;

			Assert.AreEqual(0, deps.Count);
		}
	}
}
