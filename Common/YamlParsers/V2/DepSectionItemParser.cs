using Common.YamlParsers.Models;
using JetBrains.Annotations;

namespace Common.YamlParsers.V2
{
    public class DepSectionItemParser
    {
        public DepSectionItem Parse(string line)
        {
            var treeishStartIndex = -1;
            var configStartIndex = -1;

            for (var i = 1; i < line.Length - 1; i++)
            {
                if (IsUnescapedChar(line, i, '@'))
                    treeishStartIndex = i + 1;

                if (IsUnescapedChar(line, i, '/'))
                    configStartIndex = i + 1;
            }

            var name = line;
            string treeish = null;
            string config = null;
            var l = line.Length;

            if (treeishStartIndex > 0 && configStartIndex < 0)
            {
                name = line.Substring(0, treeishStartIndex - 1);
                treeish = line.Substring(treeishStartIndex, l - treeishStartIndex);
            }
            else if (treeishStartIndex < 0 && configStartIndex > 0)
            {
                name = line.Substring(0, configStartIndex - 1);
                config = line.Substring(configStartIndex, l - configStartIndex);
            }
            else if (treeishStartIndex > 0 && configStartIndex > 0)
            {
                if (treeishStartIndex < configStartIndex)
                {
                    name = line.Substring(0, treeishStartIndex - 1);
                    treeish = line.Substring(treeishStartIndex, configStartIndex - treeishStartIndex - 1);
                    config = line.Substring(configStartIndex, l - configStartIndex);
                }
                else
                {
                    name = line.Substring(0, configStartIndex - 1);
                    treeish = line.Substring(treeishStartIndex, l - treeishStartIndex);
                    config = line.Substring(configStartIndex, treeishStartIndex - configStartIndex - 1);
                }
            }

            name = UnEscapeBadChars(name);
            treeish = UnEscapeBadChars(treeish);
            config = UnEscapeBadChars(config);

            var isRemoved = name[0] == '-';
            name = isRemoved ? name.Substring(1) : name;

            var dep = new Dep(name, treeish, config);
            return new DepSectionItem(isRemoved, dep);
        }

        private bool IsUnescapedChar(string str, int index, char c)
        {
            return str[index] == c && str[index - 1] != '\\';
        }

        private string UnEscapeBadChars([CanBeNull] string str)
        {
            return str?.Replace("\\@", "@").Replace("\\/", "/");
        }
    }
}