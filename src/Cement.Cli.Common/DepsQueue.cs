using System.Collections.Generic;
using System.Linq;

namespace Cement.Cli.Common;

// TODO: не очень понятно, зачем эта обертка нужна, нужен рефакторинг
public sealed class DepsQueue
{
    private readonly Queue<DepWithParent> queue;

    public DepsQueue()
    {
        queue = new Queue<DepWithParent>();
    }

    public bool IsEmpty()
    {
        return !queue.Any();
    }

    public DepWithParent Pop()
    {
        return queue.Dequeue();
    }

    public void AddRange(IList<Dep> deps, string parentModule = null)
    {
        if (deps == null)
            return;

        foreach (var dep in deps)
            queue.Enqueue(new DepWithParent(dep, parentModule));
    }

    public void AddRange(IList<DepWithParent> deps)
    {
        if (deps == null)
            return;

        foreach (var dep in deps)
            queue.Enqueue(dep);
    }
}
