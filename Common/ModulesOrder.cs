using System.Collections.Generic;

namespace Common
{
    public sealed class ModulesOrder
    {
        public List<Dep> BuildOrder;
        public List<Dep> UpdatedModules;
        public Dictionary<string, string> CurrentCommitHashes;
        public Dictionary<Dep, List<Dep>> ConfigsGraph;
    }
}