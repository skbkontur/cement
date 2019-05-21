using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Common.YamlParsers
{
    public class HooksSectionParser
    {
        [CanBeNull]
        public string[] Parse([CanBeNull] object hooksSection)
        {
            if (hooksSection == null)
                return null;

            var hooks = hooksSection as List<object>;
            if (hooks == null)
                return new string[0];

            return hooks.Select(h => h.ToString()).ToArray();
        }
    }
}