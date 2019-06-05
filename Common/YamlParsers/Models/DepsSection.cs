namespace Common.YamlParsers.Models
{
    public class DepsSection
    {
        public DepsSection(string[] force = null) : this(force, new DepSectionItem[0])
        {
        }

        public DepsSection(string[] force, DepSectionItem[] sectionItems)
        {
            Force = force;
            SectionItems = sectionItems;
        }

        public string[] Force { get; set; }
        public DepSectionItem[] SectionItems { get; set; }
    }
}