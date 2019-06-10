using System;
using Common.YamlParsers;
using Common.YamlParsers.Models;
using Common.YamlParsers.V2;
using NUnit.Framework;

namespace Tests.ParsersTests
{
    [TestFixture]
    public class TestConfigLineParser
    {
        [TestCaseSource(nameof(Source))]
        public ConfigSectionTitle Parse(string input)
        {
            var parser = new ConfigSectionTitleParser();
            return parser.Parse(input);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t")]
        [TestCase("*default")]
        [TestCase("\t*default")]
        public void ThrowOnInvalidInput(string input)
        {
            var parser = new ConfigSectionTitleParser();
            Assert.Throws<ArgumentException>(() => parser.Parse(input));
        }

        private static TestCaseData[] Source =
        {
            new TestCaseData("full-build").Returns(new ConfigSectionTitle()
            {
                Name = "full-build"
            }),

            new TestCaseData("full-build*default").Returns(new ConfigSectionTitle()
            {
                Name = "full-build",
                IsDefault = true
            }),

            new TestCaseData("full-build *default").Returns(new ConfigSectionTitle()
            {
                Name = "full-build",
                IsDefault = true
            }),

            new TestCaseData("full-build         *default").Returns(new ConfigSectionTitle()
            {
                Name = "full-build",
                IsDefault = true
            }),

            new TestCaseData("full-build>client").Returns(new ConfigSectionTitle()
            {
                Name = "full-build",
                Parents = new[] { "client" }
            }),

            new TestCaseData("full-build > client").Returns(new ConfigSectionTitle()
            {
                Name = "full-build",
                Parents = new[] { "client" }
            }),

            new TestCaseData("full-build     >    client").Returns(new ConfigSectionTitle()
            {
                Name = "full-build",
                Parents = new[] { "client" }
            }),

            new TestCaseData("full-build > client *default").Returns(new ConfigSectionTitle()
            {
                Name = "full-build",
                IsDefault = true,
                Parents = new[] { "client" }
            }),

            new TestCaseData("full-build   >   client   *default   ").Returns(new ConfigSectionTitle()
            {
                Name = "full-build",
                IsDefault = true,
                Parents = new[] { "client" }
            }),

            new TestCaseData("full-build > client, client, client").Returns(new ConfigSectionTitle()
            {
                Name = "full-build",
                Parents = new[] { "client" }
            }),

            new TestCaseData("full-build > a, b, c").Returns(new ConfigSectionTitle()
            {
                Name = "full-build",
                Parents = new[] { "a", "b", "c" }
            }),

            new TestCaseData("full-build > a, b, c *default").Returns(new ConfigSectionTitle()
            {
                Name = "full-build",
                Parents = new[] { "a", "b", "c" },
                IsDefault = true
            }),
        };

    }
}