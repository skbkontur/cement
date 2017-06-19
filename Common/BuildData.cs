using System.Collections.Generic;

namespace Common
{
	public class BuildData
	{
		public string Target { get; }
		public Tool Tool { get; }
 		public string Configuration { get; }
		public List<string> Parameters { get; }
		public string Name { get; }

		public BuildData(string target, string configuration)
		{
			Target = target;
			Configuration = configuration;
		}

		public BuildData(string target, string configuration, Tool tool, List<string> parameters, string name)
		{
			Target = target;
			Configuration = configuration;
			Tool = tool;
			Parameters = parameters;
			Name = name;
		}
	}

	public class Tool
	{
		public string Name { get; set; }
		public string Version { get; set; }
	}

}
