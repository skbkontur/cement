using System.Collections.Generic;
using System.Linq;
using Cement.Cli.Common.Exceptions;

namespace Cement.Cli.Common;

public sealed class TreeishManager
{
    public bool TreeishAlreadyProcessed(Dep dep, IList<Dep> processed)
    {
        return
            processed.Select(d => d.Treeish)
                .Any(
                    dtreeish =>
                        (dtreeish == null && dep.Treeish == null) ||
                        (dtreeish != null && (dtreeish.Equals(dep.Treeish) || dep.Treeish == null)));
    }

    public void ThrowOnTreeishConflict(DepWithParent depWithParent, IList<DepWithParent> processed)
    {
        var conflictDep =
            processed.FirstOrDefault(d => d.Dep.Treeish != null && !d.Dep.Treeish.Equals(depWithParent.Dep.Treeish));
        if (conflictDep != null && depWithParent.Dep.Treeish != null && conflictDep.Dep.Treeish != null && !conflictDep.Dep.Treeish.Trim().Equals(""))
        {
            throw new TreeishConflictException(
                string.Format(
                    "Treeish conflict: {0} refers to {4}:{1}, while {2} refers to {4}:{3}",
                    depWithParent.ParentModule, depWithParent.Dep.Treeish, conflictDep.ParentModule, conflictDep.Dep.Treeish, conflictDep.Dep.Name));
        }
    }
}
