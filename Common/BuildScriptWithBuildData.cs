namespace Common;

public sealed class BuildScriptWithBuildData
{
    public BuildScriptWithBuildData(string script, BuildData buildData)
    {
        Script = script;
        BuildData = buildData;
    }

    public string Script { get; }

    public BuildData BuildData { get; }
}
