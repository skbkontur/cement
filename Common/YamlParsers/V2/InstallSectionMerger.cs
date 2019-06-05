using System.Linq;
using Common.Extensions;
using JetBrains.Annotations;

namespace Common.YamlParsers.V2
{
    public class InstallSectionMerger
    {
        [NotNull]
        public InstallData Merge(InstallData currentInstalls, InstallData defaultInstalls = null, InstallData[] parentInstalls = null)
        {
            if (defaultInstalls == null && parentInstalls == null)
                return currentInstalls;

            var accumulate = defaultInstalls ?? new InstallData();
            if (parentInstalls != null)
                accumulate = parentInstalls.Aggregate(accumulate, (cur, parent) => cur.JoinWith(parent, currentInstalls.CurrentConfigurationInstallFiles));

            accumulate = accumulate.JoinWith(currentInstalls, currentInstalls.CurrentConfigurationInstallFiles);
            return accumulate;
        }
    }
}