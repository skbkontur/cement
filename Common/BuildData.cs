using System.Collections.Generic;

namespace Common
{
    public sealed class BuildData
    {
        public BuildData(string target, string configuration)
        {
            Target = target;
            Configuration = configuration;
        }

        public BuildData(string target, string configuration, Tool tool, IReadOnlyCollection<string> parameters, string name)
        {
            Target = target;
            Configuration = configuration;
            Tool = tool;
            Parameters = parameters;
            Name = name;
        }

        public string Target { get; }
        public Tool Tool { get; }
        public string Configuration { get; }
        public IReadOnlyCollection<string> Parameters { get; }
        public string Name { get; }
    }
}
