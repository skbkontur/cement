using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Common.YamlParsers.Models
{
    public class InstallSection
    {
        public InstallSection(object install, object artifacts)
        {
            Install = Transform(install);
            Artifacts = Transform(artifacts);
        }

        public InstallSection([NotNull] string[] install, [NotNull] string[] artifacts)
        {
            Install = install;
            Artifacts = artifacts;
        }

        [NotNull]
        public string[] Install { get; }

        [NotNull]
        public string[] Artifacts { get; }

        [NotNull]
        private static string[] Transform(object sectionContent)
        {
            if (sectionContent is IEnumerable<object> list)
                return list.Cast<string>().ToArray();

            return new string[0];
        }
    }
}