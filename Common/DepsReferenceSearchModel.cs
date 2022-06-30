using System.Collections.Generic;

namespace Common
{
    public sealed class DepsReferenceSearchModel
    {
        public List<InstallData> FoundReferences;
        public List<string> NotFoundInstallSection;

        public DepsReferenceSearchModel(List<InstallData> found, List<string> notFound)
        {
            FoundReferences = found;
            NotFoundInstallSection = notFound;
        }
    }
}