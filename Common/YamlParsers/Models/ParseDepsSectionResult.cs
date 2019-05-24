namespace Common.YamlParsers.Models
{
    public class ParseDepsSectionResult
    {
        public DepsContent ResultingDeps { get; set; }
        public ParsedDepsSection RawSection { get; set; }
    }
}