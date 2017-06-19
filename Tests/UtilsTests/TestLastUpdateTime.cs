using System;
using Common;
using NUnit.Framework;

namespace Tests.UtilsTests
{
	[TestFixture]
	public class TestLastUpdateTime
	{
		[Test]
		public void TestSaveCurrent()
		{
			var now = DateTime.Now;
			Helper.SaveLastUpdateTime();
			var last = Helper.GetLastUpdateTime();

			Assert.That(last >= now && last - now <= TimeSpan.FromMinutes(2));
		}
	}
}