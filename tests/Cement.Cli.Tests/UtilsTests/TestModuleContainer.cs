using Cement.Cli.Common;
using NUnit.Framework;

namespace Cement.Cli.Tests.UtilsTests;

[TestFixture]
public class TestModuleContainer
{
    [Test]
    public void TestAddToEmptyList()
    {
        var container = new ModulesContainer();
        container.Add(new DepWithParent(new Dep("module"), null));
        Assert.AreEqual(1, container.GetDepsByName("module").Count);
    }

    [Test]
    public void TestAddToNotEmptyList()
    {
        var container = new ModulesContainer();
        container.Add(new DepWithParent(new Dep("module"), null));
        container.Add(new DepWithParent(new Dep("module"), null));
        Assert.AreEqual(2, container.GetDepsByName("module").Count);
    }

    [Test]
    public void TestGetDepsByNameNoDeps()
    {
        var container = new ModulesContainer();
        Assert.AreEqual(0, container.GetDepsByName("module").Count);
    }
}
