using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Common.YamlParsers.V2
{
    public class HooksSectionParser
    {
        [NotNull]
        public string[] Parse([CanBeNull] object hooksSection)
        {
            if (hooksSection == null)
                return new string[0];

            var hooks = hooksSection as List<object>;
            if (hooks == null)
                return new string[0];

            return hooks.Select(h => h.ToString()).ToArray();
        }
    }
}