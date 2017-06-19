using System.IO;
using Commands;
using Common;
using NUnit.Framework;

namespace Tests.CommandsTests
{
	[TestFixture]
	public class TestInit
	{
		[Test]
		public void CreateCementDir()
		{
			using (var tmp = new TempDirectory())
			{
				using (new DirectoryJumper(tmp.Path))
				{
					new Init().Run(new[]{"init"});
					Assert.That(Directory.Exists(Path.Combine(tmp.Path, Helper.CementDirectory)));
				}
			}
		}
	}
}
