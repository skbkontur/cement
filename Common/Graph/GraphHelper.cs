using System.Collections.Generic;

namespace Common.Graph;

public sealed class GraphHelper
{
    public HashSet<Dep> GetChildren(Dep dep, Dictionary<Dep, List<Dep>> graph)
    {
        var result = new HashSet<Dep>();
        GetChildrenDfs(dep, graph, result);
        return result;
    }

    private static void GetChildrenDfs(Dep v, Dictionary<Dep, List<Dep>> graph, HashSet<Dep> result)
    {
        if (result.Contains(v))
            return;
        result.Add(v);

        if (!graph.ContainsKey(v))
            return;

        foreach (var u in graph[v])
        {
            GetChildrenDfs(u, graph, result);
        }
    }
}
