using System.Collections.Generic;

namespace Common.YamlParsers
{
    public sealed class DepsData
    {
        public string[] Force { get; set; }
        public List<Dep> Deps { get; set; }

        public DepsData(string[] force, List<Dep> deps)
        {
            Force = force;
            Deps = deps;
        }
    }
}