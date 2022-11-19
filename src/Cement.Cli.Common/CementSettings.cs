using System.Collections.Generic;

namespace Cement.Cli.Common;

public sealed class CementSettings
{
    public string UserName;
    public string Domain;
    public string Password;
    public string EncryptedPassword;
    public string DefaultMsBuildVersion;
    public string CementServer;
    public bool KillMsBuild = true;
    public string SelfUpdateTreeish;
    public List<Package> Packages;
    public int? MaxDegreeOfParallelism;
    public bool? IsEnabledSelfUpdate;
    public Dictionary<string, string> UserCommands;
}
