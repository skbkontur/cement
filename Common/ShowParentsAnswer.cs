using System;
using System.Collections.Generic;

namespace Common
{
    public sealed class ShowParentsAnswer
    {
        public DateTime UpdatedTime;
        public List<KeyValuePair<Dep, List<Dep>>> Items = new List<KeyValuePair<Dep, List<Dep>>>();
    }
}