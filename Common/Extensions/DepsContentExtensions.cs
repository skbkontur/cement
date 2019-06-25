using Common.YamlParsers;

namespace Common.Extensions
{
    public static class DepsContentExtensions
    {
        public static bool HasForcedBranches(this DepsData deps)
        {
            return deps.Force != null && deps.Force.Length > 0;
        }
    }
}