using System;
using System.Collections.Generic;

namespace Common
{
    public class ShowParentsAnswer
    {
        public DateTime UpdatedTime;
        public List<KeyValuePair<Dep, List<Dep>>> Items = new List<KeyValuePair<Dep, List<Dep>>>();
    }
}