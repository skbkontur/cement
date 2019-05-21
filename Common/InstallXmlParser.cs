using System.Linq;
using System.Xml.Linq;

namespace Common
{
    public class InstallXmlParser
    {
        private readonly XDocument document;
        private readonly string moduleName;

        public InstallXmlParser(string xmlContent, string moduleName)
        {
            this.moduleName = moduleName;
            document = XDocument.Parse(xmlContent);
        }

        public InstallData Get(string configuration = null)
        {
            var result = new InstallData();
            var configs = document.Descendants("install");

            if (configuration == null)
            {
                var defaultInstall = configs.FirstOrDefault(c => !c.HasAttributes);
                if (defaultInstall == null)
                    throw new NoSuchConfigurationException(moduleName, "default");
                result.CurrentConfigurationInstallFiles = defaultInstall.Elements("add-ref")
                    .Select(e => e.FirstAttribute.Value).ToList();
                return result;
            }

            var config = configs.FirstOrDefault(c => c.Attribute("target") != null && c.Attribute("target").Value == configuration);
            if (config == null)
            {
                throw new NoSuchConfigurationException(moduleName, configuration);
            }
            result.CurrentConfigurationInstallFiles = config.Elements("add-ref")
                .Select(e => e.FirstAttribute.Value).ToList();
            return result;
        }
    }
}