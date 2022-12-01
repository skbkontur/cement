using System;
using System.Collections.Generic;

namespace Cement.Cli.Common;

public sealed class ShowParentsAnswer
{
    public DateTime UpdatedTime;
    public List<KeyValuePair<Dep, List<Dep>>> Items = new();
}
