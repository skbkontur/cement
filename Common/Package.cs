namespace Common
{
    public class Package
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Type { get; set; }

        public Package(string name, string url, string type = "git")
        {
            Name = name;
            Url = url;
            Type = type;
        }
    }
}
