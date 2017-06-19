using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Common
{
    public static class ArborJs
    {
        public static void Show(string moduleName, List<string> lines)
        {
            var arborDir = Path.Combine(Helper.GetCementInstallDirectory(), "dotnet", "arborjs");
            var templateFileName = Path.Combine(arborDir, "deps_template.html");
            var resultFileName = Path.Combine(arborDir, "deps.html");

            if (!File.Exists(templateFileName))
            {
                throw new CementException("deps_template.html not found");
            }
            var text = File.ReadAllText(templateFileName);
            text = text.Replace("<textarea id=\"code\" style=\"\"></textarea>",
                $"<textarea id=\"code\" style=\"\">{string.Join("\n", lines)}</textarea>");
            text = text.Replace("$module_name", moduleName);

            File.WriteAllText(resultFileName, text);

            Process.Start(resultFileName);
        } 
    }
}
