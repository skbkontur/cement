using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Common.YamlParsers
{
    public class ModuleYamlFile
    {
        public readonly List<string> Lines;
        private readonly string lineEndings;

        private ModuleYamlFile(string moduleYamlContent)
        {
            lineEndings = moduleYamlContent.Contains("\r\n") ? "\r\n" : "\n";
            Lines = moduleYamlContent.Split(new[] {"\r\n", "\n"}, StringSplitOptions.None).ToList();
        }

        public ModuleYamlFile(FileInfo moduleYamlPath) : this(File.ReadAllText(moduleYamlPath.FullName))
        {
        }

        private void Save(string path)
        {
            File.WriteAllText(path, Lines.Aggregate((x, y) => x + lineEndings + y));
        }

        public void Save(string path, List<string> newLines)
        {
            File.WriteAllText(path, string.Join(lineEndings, newLines));
        }

        public static void ReplaceTabs(string yamlPath)
        {
            var file = new ModuleYamlFile(new FileInfo(yamlPath));
            for (int i = 0; i < file.Lines.Count; i++)
                file.Lines[i] = file.Lines[i].Replace("\t", "    ");
            file.Save(yamlPath);
        }
    }
}
