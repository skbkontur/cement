using System.Collections.Generic;

namespace Common.YamlParsers;

public sealed class DepsData
{
    public DepsData(string[] force, List<Dep> deps)
    {
        Force = force;
        Deps = deps;
    }

    public string[] Force { get; set; }
    public List<Dep> Deps { get; set; }
}
