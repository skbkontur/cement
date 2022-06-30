using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.DepsValidators;

public sealed class DepsValidator : IDepsValidator
{
    private readonly string configurationName;

    public DepsValidator(string configurationName)
    {
        this.configurationName = configurationName;
    }

    public DepsValidateResult Validate(IEnumerable<Dep> deps, out IReadOnlyCollection<string> validateErrors)
    {
        var duplicatedDeps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var uniqueDeps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dep in deps)
        {
            if (uniqueDeps.Contains(dep.Name))
            {
                duplicatedDeps.Add(dep.Name);
                continue;
            }

            uniqueDeps.Add(dep.Name);
        }

        if (duplicatedDeps.Count == 0)
        {
            validateErrors = Array.Empty<string>();
            return DepsValidateResult.Valid;
        }

        validateErrors = duplicatedDeps
            .Select(dep => $"The dependency '{dep}' is declared several times within the '{configurationName}' configuration")
            .ToArray();

        return DepsValidateResult.Invalid;
    }
}
