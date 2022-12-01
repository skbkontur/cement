using System.Collections.Generic;

namespace Cement.Cli.Commands;

internal sealed class FixReferenceResult
{
    public Dictionary<string, List<string>> NotFound { get; } = new();
    public Dictionary<string, List<string>> Replaced { get; } = new();
    public HashSet<string> NoYamlModules { get; } = new();
}
