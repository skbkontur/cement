using System.Collections.Generic;

namespace Common.DepsValidators;

public interface IDepsValidator
{
    DepsValidateResult Validate(IEnumerable<Dep> deps, out IReadOnlyCollection<string> validateErrors);
}
