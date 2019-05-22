using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Common.YamlParsers.Models
{
    public class YamlInstallSections
    {
        public YamlInstallSections(object install, object artifacts, object artefacts)
        {
            Install = Transform(install);
            Artifacts = Transform(artifacts);
            Artefacts = Transform(artefacts);
        }

        public YamlInstallSections([NotNull] string[] install, [NotNull] string[] artifacts, [NotNull] string[] artefacts)
        {
            Install = install;
            Artifacts = artifacts;
            Artefacts = artefacts;
        }

        [NotNull]
        public string[] Install { get; }

        [NotNull]
        public string[] Artifacts { get; }

        [NotNull]
        public string[] Artefacts { get; }

        [NotNull]
        private static string[] Transform(object sectionContent)
        {
            if (sectionContent is IEnumerable<object> list)
                return list.Cast<string>().ToArray();

            return new string[0];
        }
    }
}