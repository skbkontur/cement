namespace Common
{
    public sealed class BuildScriptWithBuildData
    {
        public readonly string Script;
        public readonly BuildData BuildData;

        public BuildScriptWithBuildData(string script, BuildData buildData)
        {
            Script = script;
            BuildData = buildData;
        }
    }
}
