using System;
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
                Artifacts = new List<string>(ConcatDistinct(first, second, item => item.Artifacts)),
                ExternalModules = new List<string>(ConcatDistinct(first, second, item => item.ExternalModules)),
                InstallFiles = new List<string>(ConcatDistinct(first, second, item => item.InstallFiles)),
                NuGetPackages = new List<string>(ConcatDistinct(first, second, item => item.NuGetPackages)),
                CurrentConfigurationInstallFiles = new List<string>(ConcatDistinct(first, second, item => item.CurrentConfigurationInstallFiles)),
            };

            return result;
        }

        private static IEnumerable<string> ConcatDistinct(InstallData a, InstallData b, Func<InstallData, IEnumerable<string>> getCollectionFunc)
        {
            var firstCollection = getCollectionFunc(a);
            var secondCollection = getCollectionFunc(b);
            return firstCollection.Concat(secondCollection).Distinct();
        }
    }
}