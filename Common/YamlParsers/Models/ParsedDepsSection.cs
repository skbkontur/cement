namespace Common.YamlParsers.Models
{
    public class ParsedDepsSection
    {
        public ParsedDepsSection(string[] force = null) : this(force, new DepLine[0])
        {
        }

        public ParsedDepsSection(string[] force, DepLine[] lines)
        {
            Force = force;
            Lines = lines;
        }

        public string[] Force { get; set; }
        public DepLine[] Lines { get; set; }
    }
}