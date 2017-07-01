using System;
using System.Collections.Generic;
using Common;

namespace Commands
{
    public class ShowParentsAnswer
    {
        public DateTime UpdatedTime;
        public List<KeyValuePair<Dep, List<Dep>>> Items = new List<KeyValuePair<Dep, List<Dep>>>();
    }
}