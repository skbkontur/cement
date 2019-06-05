using Common;
using Common.YamlParsers.Models;
using Common.YamlParsers.V2;
using NUnit.Framework;

namespace Tests.ParsersTests.V2
{
    [TestFixture]
    public class TestDepSectionItemParser
    {
        [TestCaseSource(nameof(Source))]
        public DepSectionItem Parse(string input)
        {
            var parser = new DepSectionItemParser();
            return parser.Parse(input);
        }

        private static TestCaseData[] Source =
        {
            new TestCaseData("module").Returns(new DepSectionItem(new Dep("module"))),
            new TestCaseData("module@branch").Returns(new DepSectionItem(new Dep("module", "branch"))),
            new TestCaseData(@"module@feature\/branch").Returns(new DepSectionItem(new Dep("module", "feature/branch"))),
            new TestCaseData(@"module@feature\branch").Returns(new DepSectionItem(new Dep("module", @"feature\branch"))),
            new TestCaseData(@"module@feature\@branch").Returns(new DepSectionItem(new Dep("module", @"feature@branch"))),
            new TestCaseData("module@$CURRENT_BRANCH").Returns(new DepSectionItem(new Dep("module", "$CURRENT_BRANCH"))),
            new TestCaseData("module@v1.0.54").Returns(new DepSectionItem(new Dep("module", "v1.0.54"))),
            new TestCaseData("module@b32742e9701aef44ee986db2824e9007056ba60f")
                .Returns(new DepSectionItem(new Dep("module", "b32742e9701aef44ee986db2824e9007056ba60f"))),
            new TestCaseData("module/some-cfg")
                .Returns(new DepSectionItem(new Dep("module", null, "some-cfg"))),
            new TestCaseData("module@branch/some-cfg")
                .Returns(new DepSectionItem(new Dep("module", "branch", "some-cfg"))),
            new TestCaseData(@"module@feature\/branch/some-cfg")
                .Returns(new DepSectionItem(new Dep("module", @"feature/branch", "some-cfg"))),
            new TestCaseData(@"module@feature\branch/some-cfg")
                .Returns(new DepSectionItem(new Dep("module", @"feature\branch", "some-cfg"))),
            new TestCaseData(@"module@feature\@branch/some-cfg")
                .Returns(new DepSectionItem(new Dep("module", @"feature@branch", "some-cfg"))),
            new TestCaseData("module@$CURRENT_BRANCH/some-cfg")
                .Returns(new DepSectionItem(new Dep("module", "$CURRENT_BRANCH", "some-cfg"))),
            new TestCaseData("module@v1.0.54/some-cfg")
                .Returns(new DepSectionItem(new Dep("module", "v1.0.54", "some-cfg"))),
            new TestCaseData("module@b32742e9701aef44ee986db2824e9007056ba60f/some-cfg")
                .Returns(new DepSectionItem(new Dep("module", "b32742e9701aef44ee986db2824e9007056ba60f", "some-cfg"))),
            new TestCaseData("module/some-cfg@branch")
                .Returns(new DepSectionItem(new Dep("module", "branch", "some-cfg"))),
        };
    }
}