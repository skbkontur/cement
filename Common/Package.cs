namespace Common;

public sealed class Package
{
    public Package(string name, string url, string type = "git")
    {
        Name = name;
        Url = url;
        Type = type;
    }

    public string Name { get; set; }
    public string Url { get; set; }
    public string Type { get; set; }
}
