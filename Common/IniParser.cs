using System;
using System.Collections.Generic;
using System.Linq;

namespace Common
{
    public sealed class IniParser
    {
        public IniParser()
        {
            ini = new Dictionary<string, Dictionary<string, string>>(StringComparer.InvariantCultureIgnoreCase);
        }

        private readonly Dictionary<string, Dictionary<string, string>> ini;

        public IniData ParseString(string txt)
        {
            ini.Clear();
            var currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            ini[""] = currentSection;
            string currentOption = "";

            foreach (var line in txt.Split(new[] {"\n"}, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim()))
            {
                if (line.StartsWith(";") || line.StartsWith("#"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentOption = "";
                    currentSection = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                    ini[line.Substring(1, line.LastIndexOf("]") - 1)] = currentSection;
                    continue;
                }

                var idx = line.IndexOf("=");
                if (idx == -1)
                {
                    if (currentOption != "")
                        currentSection[currentOption] += "\r\n" + line;
                }
                else
                {
                    currentSection[line.Substring(0, idx).Trim()] = line.Substring(idx + 1).Trim();
                    currentOption = line.Substring(0, idx).Trim();
                }
            }
            return new IniData(ini);
        }
    }
}