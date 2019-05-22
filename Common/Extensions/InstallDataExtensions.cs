using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Common.Extensions
{
    public static class InstallDataExtensions
    {
        /// <summary>
        /// Returns new instance of InstallData with properties copied from first and second InstallData
        /// </summary>
        public static InstallData JoinWith(this InstallData first, [NotNull] InstallData second)
        {
            var result = new InstallData
            {
                Artifacts = new List<string>(first.Artifacts.Concat(second.Artifacts)),
                ExternalModules = new List<string>(first.ExternalModules.Concat(second.ExternalModules)),
                InstallFiles = new List<string>(first.InstallFiles.Concat(second.InstallFiles)),
                NuGetPackages = new List<string>(first.NuGetPackages.Concat(second.NuGetPackages)),
                CurrentConfigurationInstallFiles = new List<string>(first.CurrentConfigurationInstallFiles.Concat(second.CurrentConfigurationInstallFiles)),
            };

            return result;
        }
    }
}