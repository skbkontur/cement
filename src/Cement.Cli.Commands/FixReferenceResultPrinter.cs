using System.Linq;
using Cement.Cli.Common;

namespace Cement.Cli.Commands;

internal sealed class FixReferenceResultPrinter
{
    private readonly ConsoleWriter consoleWriter;

    public FixReferenceResultPrinter(ConsoleWriter consoleWriter)
    {
        this.consoleWriter = consoleWriter;
    }

    public void Print(FixReferenceResult result)
    {
        if (result.NoYamlModules.Any())
        {
            consoleWriter.WriteWarning("No 'install' section in modules:");
            foreach (var m in result.NoYamlModules)
                consoleWriter.WriteBuildWarning("\t- " + m);
        }

        foreach (var key in result.Replaced.Keys)
        {
            if (!result.Replaced[key].Any())
                continue;
            consoleWriter.WriteOk(key + " replaces:");
            foreach (var value in result.Replaced[key])
                consoleWriter.WriteLine("\t" + value);
        }

        foreach (var key in result.NotFound.Keys)
        {
            if (!result.NotFound[key].Any())
                continue;
            consoleWriter.WriteError(key + "\n\tnot found references in install/artifacts section of any module:");
            foreach (var value in result.NotFound[key])
                consoleWriter.WriteLine("\t" + value);
        }
    }
}
