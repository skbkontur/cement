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
        public ConfigurationLine Parse(string input)
        {
            var parser = new ConfigLineParser();
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
            var parser = new ConfigLineParser();
            Assert.Throws<ArgumentException>(() => parser.Parse(input));
        }

        private TestCaseData[] Source =
        {
            new TestCaseData("full-build").Returns(new ConfigurationLine()
            {
                ConfigName = "full-build"
            }),

            new TestCaseData("full-build*default").Returns(new ConfigurationLine()
            {
                ConfigName = "full-build",
                IsDefault = true
            }),

            new TestCaseData("full-build *default").Returns(new ConfigurationLine()
            {
                ConfigName = "full-build",
                IsDefault = true
            }),

            new TestCaseData("full-build         *default").Returns(new ConfigurationLine()
            {
                ConfigName = "full-build",
                IsDefault = true
            }),

            new TestCaseData("full-build>client").Returns(new ConfigurationLine()
            {
                ConfigName = "full-build",
                ParentConfigs = new[] { "client" }
            }),

            new TestCaseData("full-build > client").Returns(new ConfigurationLine()
            {
                ConfigName = "full-build",
                ParentConfigs = new[] { "client" }
            }),

            new TestCaseData("full-build     >    client").Returns(new ConfigurationLine()
            {
                ConfigName = "full-build",
                ParentConfigs = new[] { "client" }
            }),

            new TestCaseData("full-build > client *default").Returns(new ConfigurationLine()
            {
                ConfigName = "full-build",
                IsDefault = true,
                ParentConfigs = new[] { "client" }
            }),

            new TestCaseData("full-build   >   client   *default   ").Returns(new ConfigurationLine()
            {
                ConfigName = "full-build",
                IsDefault = true,
                ParentConfigs = new[] { "client" }
            }),

            new TestCaseData("full-build > client, client, client").Returns(new ConfigurationLine()
            {
                ConfigName = "full-build",
                ParentConfigs = new[] { "client" }
            }),

            new TestCaseData("full-build > a, b, c").Returns(new ConfigurationLine()
            {
                ConfigName = "full-build",
                ParentConfigs = new[] { "a", "b", "c" }
            }),

            new TestCaseData("full-build > a, b, c *default").Returns(new ConfigurationLine()
            {
                ConfigName = "full-build",
                ParentConfigs = new[] { "a", "b", "c" },
                IsDefault = true
            }),
        };

    }
}