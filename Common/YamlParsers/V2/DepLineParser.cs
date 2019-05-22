using JetBrains.Annotations;

namespace Common.YamlParsers.V2
{
    public class DepLineParser
    {
        public Dep Parse(string line)
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

            return new Dep(UnEscapeBadChars(name), UnEscapeBadChars(treeish), UnEscapeBadChars(config));
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