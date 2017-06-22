using System;

namespace Common
{
    public class Module
    {
        public string Name { get; }
        public string Url { get; }
        public string Pushurl { get; }
        public string Type { get; }

        public Module(string name, string url, string pushurl)
        {
            Name = name;
            Url = url;
            Pushurl = pushurl;
        }

        public Module(IniData parsedData, string sectionName)
        {
            var sectionNameTokens = sectionName.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            if (sectionNameTokens.Length < 2 || !sectionNameTokens[0].Equals("module"))
            {
                throw new CementException("Section is not module: " + sectionName);
            }
            Name = sectionNameTokens[1];
            Type = parsedData.GetValue("type", sectionName).Equals("")
                ? "git"
                : parsedData.GetValue("type", sectionName);
            if (parsedData.GetValue("url", sectionName).Equals(""))
            {
                throw new CementException("Missing url in module: " + sectionName);
            }
            Url = parsedData.GetValue("url", sectionName);
            Pushurl = parsedData.GetValue("pushurl", sectionName).Equals("")
                ? null
                : parsedData.GetValue("pushurl", sectionName);
        }
    }
}
