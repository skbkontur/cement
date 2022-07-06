using System.IO;

namespace Common
{
    public static class InstallParser
    {
        public static InstallData Get(string module, string configuration)
        {
            var yamlSpecFile = Path.Combine(Helper.CurrentWorkspace, module, Helper.YamlSpecFile);
            var xmlSpecFile = Path.Combine(Helper.CurrentWorkspace, module, ".cm", "spec.xml");
            return File.Exists(yamlSpecFile)
                ? new InstallCollector(Directory.GetParent(yamlSpecFile).FullName).Get(configuration)
                : File.Exists(xmlSpecFile)
                    ? new InstallXmlParser(File.ReadAllText(xmlSpecFile), module).Get(configuration)
                    : new InstallData();
        }
    }
}
