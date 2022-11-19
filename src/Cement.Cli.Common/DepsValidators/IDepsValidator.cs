using System.Collections.Generic;

namespace Cement.Cli.Common.DepsValidators;

public interface IDepsValidator
{
    DepsValidateResult Validate(IEnumerable<Dep> deps, out IReadOnlyCollection<string> validateErrors);
}
