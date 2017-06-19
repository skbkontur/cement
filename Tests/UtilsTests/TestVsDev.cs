using System;
using Common;
using NUnit.Framework;

namespace Tests.UtilsTests
{
	[TestFixture]
	public class TestVsDev
	{
		[Test]
		public void TestReplaceToVsVariables()
		{
			VsDevHelper.ReplaceVariablesToVs();
			var vars = VsDevHelper.GetCurrentSetVariables();

			Assert.That(vars.ContainsKey("USERPROFILE"));
			Assert.That(vars.ContainsKey("UCRTVersion"));

			var runner = new ShellRunner();
			runner.Run("set");
			var after = runner.Output;
			Assert.That(after.IndexOf("UCRTVersion", StringComparison.Ordinal) >= 0);
		}
	}
}