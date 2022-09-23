using System.Collections.Generic;
using Common.Exceptions;

namespace Common
{
    public static class ModuleIniParser
    {
        public static Module[] Parse(string iniFileData)
        {
            var result = new List<Module>();
            var parsedData = new IniParser().ParseString(iniFileData);
            foreach (var section in parsedData.GetSections())
            {
                try
                {
                    var module = new Module(parsedData, section);
                    result.Add(module);
                }
                catch (CementException)
                {
                }
            }

            return result.ToArray();
        }
    }
}
